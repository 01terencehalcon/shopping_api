'use client';

import React, { useState, useEffect } from 'react';
import { useApp } from '../lib/context';
import { api, resolveImageUrl } from '../lib/api';
import StoreHeader from '../components/StoreHeader';
import Footer from '../components/Footer';
import LocationPickerMap from '../components/LocationPickerMap';
import {
  ChevronRight,
  ShoppingBag,
  ShieldCheck,
  CreditCard,
  Truck,
  Wallet,
  CheckCircle2,
  ArrowRight,
  MapPin,
  Plus,
  AlertCircle
} from 'lucide-react';

export default function CheckoutScreen() {
  const {
    cartItems,
    subtotal,
    discountAmount,
    shippingFee,
    total,
    clearCart,
    user,
    isLoggedIn,
    isAuthLoading,
    setRedirectAfterLogin,
    navigateTo,
    showToast
  } = useApp();

  const [payMethod, setPayMethod] = useState('cod');
  const [placedOrderId, setPlacedOrderId] = useState(null);
  const [placedOrderSnapshot, setPlacedOrderSnapshot] = useState(null);
  const [placing, setPlacing] = useState(false);

  // Saved addresses state
  const [savedAddresses, setSavedAddresses] = useState([]);
  const [loadingAddresses, setLoadingAddresses] = useState(true);
  const [addressApiError, setAddressApiError] = useState(null);
  const [selectedAddressId, setSelectedAddressId] = useState('new'); // id or 'new'

  // New address form fields (all start empty, placeholders only)
  const [newAddress, setNewAddress] = useState({
    recipientName: user?.username || '',
    phone: user?.phone || '',
    street: '',
    city: '',
    province: '',
    postalCode: '',
    latitude: null,
    longitude: null,
  });

  const [cardData, setCardData] = useState({
    number: '',
    expiry: '',
    cvv: '',
  });

  // Redirect if not logged in
  useEffect(() => {
    if (!isAuthLoading && !isLoggedIn) {
      setRedirectAfterLogin('checkout');
      showToast("Please log in to continue", "info");
      navigateTo('login');
    }
  }, [isLoggedIn, isAuthLoading, setRedirectAfterLogin, showToast, navigateTo]);

  // Load saved addresses on mount
  useEffect(() => {
    let mounted = true;
    async function fetchAddresses() {
      if (!user?.userId) return;
      setLoadingAddresses(true);
      setAddressApiError(null);

      try {
        const res = await api.addresses.getByUser(user.userId);
        if (!mounted) return;

        if (res && res.success && Array.isArray(res.data)) {
          setSavedAddresses(res.data);
          if (res.data.length > 0) {
            // Find first address with coordinates if available, else first address
            const withCoords = res.data.find(a => typeof a.latitude === 'number' && typeof a.longitude === 'number');
            setSelectedAddressId(withCoords ? withCoords.addressId : res.data[0].addressId);
          } else {
            setSelectedAddressId('new');
          }
        } else if (Array.isArray(res)) {
          // In case array returned directly
          setSavedAddresses(res);
          if (res.length > 0) {
            setSelectedAddressId(res[0].addressId);
          } else {
            setSelectedAddressId('new');
          }
        } else {
          setAddressApiError(res?.error || 'Unable to connect to the server to load saved addresses.');
          setSelectedAddressId('new');
        }
      } catch (err) {
        if (mounted) {
          setAddressApiError('Unable to connect to the server to load saved addresses.');
          setSelectedAddressId('new');
        }
      } finally {
        if (mounted) setLoadingAddresses(false);
      }
    }

    if (isLoggedIn && user?.userId) {
      fetchAddresses();
    }
  }, [isLoggedIn, user]);

  const handleNewAddressChange = (e) => {
    const { name, value } = e.target;
    setNewAddress(prev => ({ ...prev, [name]: value }));
  };

  const handlePlaceOrder = async (e) => {
    e.preventDefault();
    if (cartItems.length === 0) {
      showToast("Your cart is empty!", "error");
      navigateTo('listing');
      return;
    }

    let finalAddressId = null;
    let chosenSnapshot = null;

    if (selectedAddressId === 'new') {
      // Validate new address required fields
      if (!newAddress.recipientName.trim()) {
        showToast("Please enter the recipient name.", "error");
        return;
      }
      if (!newAddress.phone.trim()) {
        showToast("Please enter a contact phone number.", "error");
        return;
      }
      if (!newAddress.street.trim()) {
        showToast("Please enter the street address.", "error");
        return;
      }
      if (!newAddress.city.trim()) {
        showToast("Please enter the city.", "error");
        return;
      }

      // Rule 6 & 8: Checkout cannot be submitted without coordinates
      if (
        typeof newAddress.latitude !== 'number' ||
        typeof newAddress.longitude !== 'number' ||
        isNaN(newAddress.latitude) ||
        isNaN(newAddress.longitude)
      ) {
        showToast("Please pin your exact delivery location on the map before proceeding.", "error");
        return;
      }

      setPlacing(true);

      // Create address first (Fix 8)
      try {
        const addrPayload = {
          userId: user?.userId,
          recipientName: newAddress.recipientName.trim(),
          phone: newAddress.phone.trim(),
          street: newAddress.street.trim(),
          city: newAddress.city.trim(),
          province: newAddress.province.trim(),
          postalCode: newAddress.postalCode.trim(),
          latitude: newAddress.latitude,
          longitude: newAddress.longitude,
        };

        const addrRes = await api.addresses.create(addrPayload);
        if (!addrRes || !addrRes.success) {
          showToast(addrRes?.error || "Failed to save delivery address. Order not placed.", "error");
          setPlacing(false);
          return;
        }

        finalAddressId = addrRes.data?.addressId || addrRes.data?.id || addrRes.id;
        chosenSnapshot = {
          recipient: newAddress.recipientName.trim(),
          phone: newAddress.phone.trim(),
          street: newAddress.street.trim(),
          city: newAddress.city.trim(),
          province: newAddress.province.trim(),
        };
      } catch (err) {
        console.error("Address creation failed:", err);
        showToast("Failed to save delivery address on server. Order not placed.", "error");
        setPlacing(false);
        return;
      }
    } else {
      // Using an existing saved address
      const chosen = savedAddresses.find(a => a.addressId === Number(selectedAddressId));
      if (!chosen) {
        showToast("Selected address not found. Please choose an address.", "error");
        return;
      }

      if (
        typeof chosen.latitude !== 'number' ||
        typeof chosen.longitude !== 'number' ||
        isNaN(chosen.latitude) ||
        isNaN(chosen.longitude)
      ) {
        showToast("The selected saved address has no map coordinates. Please use '+ Add a new address' with a map pin to proceed.", "error");
        return;
      }

      finalAddressId = chosen.addressId;
      chosenSnapshot = {
        recipient: chosen.recipientName,
        phone: chosen.phone,
        street: chosen.street,
        city: chosen.city,
        province: chosen.province,
      };
      setPlacing(true);
    }

    // Now create the order with addressId (Fix 8)
    try {
      const orderPayload = {
        userId: user?.userId,
        addressId: finalAddressId,
        total: Number(total.toFixed(2)),
        status: "Processing",
        items: cartItems.map(item => ({
          productId: item.productId,
          quantity: item.quantity,
          price: item.price,
        }))
      };

      const orderRes = await api.orders.create(orderPayload);
      if (!orderRes || !orderRes.success) {
        showToast(orderRes?.error || "Failed to place order on server. Cart has been preserved.", "error");
        setPlacing(false);
        return;
      }

      const orderId = orderRes.data?.id || orderRes.data?.orderId || orderRes.id;
      setPlacedOrderId(orderId);
      setPlacedOrderSnapshot(chosenSnapshot);
      clearCart();
      showToast(`Order #ORD-${orderId} placed successfully!`);
    } catch (err) {
      console.error("Order creation failed:", err);
      showToast("Unable to submit order to server. Cart has been preserved.", "error");
    } finally {
      setPlacing(false);
    }
  };

  if (isAuthLoading || !isLoggedIn) {
    return null;
  }

  // ─── Order Placed Success Screen ───────────────────────────────────────────
  if (placedOrderId) {
    return (
      <div className="min-h-screen bg-gray-50 flex flex-col">
        <StoreHeader />
        <div className="flex-1 flex items-center justify-center p-6">
          <div className="bg-white border border-gray-200 rounded-2xl p-8 sm:p-10 text-center max-w-md w-full shadow-sm animate-in zoom-in-95">
            <div className="w-16 h-16 bg-emerald-100 text-emerald-600 rounded-full flex items-center justify-center mx-auto mb-5">
              <CheckCircle2 size={32} />
            </div>
            
            <span className="text-[11px] font-bold uppercase tracking-widest text-emerald-700 bg-emerald-50 px-2.5 py-1 rounded-full border border-emerald-200">
              Payment Confirmed
            </span>

            <h1 className="text-2xl font-black text-gray-900 mt-3 mb-2">Order Confirmed!</h1>
            <p className="text-xs text-gray-500 mb-6 leading-relaxed">
              Thank you for choosing StyleShop. Your order <span className="font-mono font-bold text-gray-800">#ORD-{placedOrderId}</span> has been processed and sent to fulfillment.
            </p>

            <div className="bg-gray-50 border border-gray-200 rounded-xl p-4 text-left text-xs space-y-2 mb-6">
              <div className="flex justify-between">
                <span className="text-gray-500">Estimated Delivery:</span>
                <span className="font-semibold text-gray-800">3-5 Business Days</span>
              </div>
              {placedOrderSnapshot && (
                <>
                  <div className="flex justify-between">
                    <span className="text-gray-500">Recipient:</span>
                    <span className="font-semibold text-gray-800">{placedOrderSnapshot.recipient}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-500">Shipping To:</span>
                    <span className="font-semibold text-gray-800 truncate max-w-[200px]">
                      {placedOrderSnapshot.street}, {placedOrderSnapshot.city}
                    </span>
                  </div>
                </>
              )}
              <div className="flex justify-between">
                <span className="text-gray-500">Payment:</span>
                <span className="font-semibold text-gray-800 uppercase">{payMethod}</span>
              </div>
            </div>

            <div className="space-y-2">
              <button
                onClick={() => navigateTo('tracking', { orderId: placedOrderId })}
                className="w-full py-2.5 bg-gray-900 hover:bg-gray-800 text-white font-bold text-xs rounded-xl shadow-xs transition cursor-pointer flex items-center justify-center gap-2"
              >
                <MapPin size={14} />
                <span>Track Live Delivery Map</span>
              </button>
              <button
                onClick={() => navigateTo('profile')}
                className="w-full py-2.5 bg-white hover:bg-gray-100 text-gray-700 font-semibold text-xs rounded-xl border border-gray-300 transition cursor-pointer"
              >
                View in Order History
              </button>
              <button
                onClick={() => navigateTo('home')}
                className="w-full py-2 text-gray-500 hover:text-gray-800 font-semibold text-xs transition cursor-pointer"
              >
                Back to Storefront
              </button>
            </div>
          </div>
        </div>
        <Footer />
      </div>
    );
  }

  // ─── Main Checkout Form ───────────────────────────────────────────────────
  return (
    <div className="min-h-screen bg-gray-50 flex flex-col">
      <StoreHeader />

      <div className="max-w-5xl mx-auto px-4 sm:px-6 py-8 flex-1 w-full">
        
        {/* Stepper Header */}
        <div className="flex items-center gap-3 sm:gap-6 mb-8 pb-4 border-b border-gray-200 overflow-x-auto">
          {[
            { step: 1, label: "Cart", completed: true },
            { step: 2, label: "Shipping", active: true },
            { step: 3, label: "Payment", active: true },
            { step: 4, label: "Confirmation", completed: false },
          ].map((item, idx) => (
            <div key={item.label} className="flex items-center gap-2 flex-shrink-0">
              <div
                className={`w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold ${
                  item.completed
                    ? 'bg-emerald-600 text-white'
                    : item.active
                    ? 'bg-gray-900 text-white'
                    : 'bg-gray-200 text-gray-500'
                }`}
              >
                {item.completed ? '✓' : item.step}
              </div>
              <span className={`text-xs font-bold ${item.active ? 'text-gray-900' : 'text-gray-400'}`}>
                {item.label}
              </span>
              {idx < 3 && <ChevronRight size={14} className="text-gray-300 ml-2" />}
            </div>
          ))}
        </div>

        <form onSubmit={handlePlaceOrder}>
          <div className="flex flex-col lg:flex-row gap-8">
            
            {/* Left Column: Shipping & Payment forms */}
            <div className="flex-1 space-y-6">
              
              {/* Shipping Information Card */}
              <div className="bg-white border border-gray-200 rounded-xl p-6 shadow-2xs space-y-5">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2 text-sm font-bold text-gray-900">
                    <div className="w-5 h-5 bg-gray-900 text-white rounded-full flex items-center justify-center text-xs">
                      1
                    </div>
                    <h2>Delivery Destination</h2>
                  </div>
                  {savedAddresses.length > 0 && (
                    <span className="text-[11px] text-gray-500">
                      {savedAddresses.length} saved address{savedAddresses.length > 1 ? 'es' : ''}
                    </span>
                  )}
                </div>

                {/* API connection error notification */}
                {addressApiError && (
                  <div className="p-4 bg-red-50 border border-red-200 text-red-700 text-xs rounded-xl flex items-center gap-2">
                    <AlertCircle size={15} className="flex-shrink-0" />
                    <span>{addressApiError}</span>
                  </div>
                )}

                {/* Address Selection Options (Fix 6) */}
                {savedAddresses.length > 0 && (
                  <div className="space-y-3 pt-1">
                    <label className="text-xs font-bold text-gray-700 uppercase tracking-wider">
                      Select Delivery Address
                    </label>
                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                      {savedAddresses.map((addr) => {
                        const isSelected = selectedAddressId === addr.addressId;
                        const hasCoords = typeof addr.latitude === 'number' && typeof addr.longitude === 'number';

                        return (
                          <div
                            key={addr.addressId}
                            onClick={() => setSelectedAddressId(addr.addressId)}
                            className={`p-4 border rounded-xl cursor-pointer transition text-xs flex flex-col justify-between ${
                              isSelected
                                ? 'border-gray-900 bg-gray-50/80 shadow-xs ring-1 ring-gray-900'
                                : 'border-gray-200 hover:border-gray-300 bg-white'
                            }`}
                          >
                            <div>
                              <div className="flex items-center justify-between mb-1.5">
                                <span className="font-bold text-gray-900 truncate">{addr.recipientName}</span>
                                <input
                                  type="radio"
                                  name="savedAddress"
                                  checked={isSelected}
                                  onChange={() => setSelectedAddressId(addr.addressId)}
                                  className="text-gray-900 focus:ring-gray-900"
                                />
                              </div>
                              <p className="text-gray-600 leading-snug truncate">{addr.street}</p>
                              <p className="text-gray-600 truncate">{addr.city}, {addr.province} {addr.postalCode}</p>
                              <p className="text-[11px] text-gray-400 mt-1">{addr.phone}</p>
                            </div>

                            <div className="mt-3 pt-2 border-t border-gray-100 flex items-center justify-between">
                              {hasCoords ? (
                                <span className="text-[10px] font-semibold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded border border-emerald-200 flex items-center gap-1 font-mono">
                                  <MapPin size={10} />
                                  <span>{addr.latitude.toFixed(3)}, {addr.longitude.toFixed(3)}</span>
                                </span>
                              ) : (
                                <span className="text-[10px] font-semibold text-amber-700 bg-amber-50 px-2 py-0.5 rounded border border-amber-200 flex items-center gap-1">
                                  <AlertCircle size={10} />
                                  <span>Missing map pin</span>
                                </span>
                              )}
                            </div>
                          </div>
                        );
                      })}

                      {/* "+ Add a new address" card */}
                      <div
                        onClick={() => setSelectedAddressId('new')}
                        className={`p-4 border rounded-xl cursor-pointer transition text-xs flex flex-col items-center justify-center text-center gap-2 min-h-[120px] ${
                          selectedAddressId === 'new'
                            ? 'border-gray-900 bg-gray-50/80 shadow-xs ring-1 ring-gray-900'
                            : 'border-dashed border-gray-300 hover:border-gray-400 bg-white'
                        }`}
                      >
                        <div className="w-8 h-8 rounded-full bg-gray-100 text-gray-700 flex items-center justify-center">
                          <Plus size={16} />
                        </div>
                        <span className="font-bold text-gray-900">+ Add a New Address</span>
                        <span className="text-[11px] text-gray-500">Pin a custom location on the map</span>
                      </div>
                    </div>
                  </div>
                )}

                {/* If selected saved address lacks coordinates, show warning */}
                {selectedAddressId !== 'new' && (() => {
                  const chosen = savedAddresses.find(a => a.addressId === Number(selectedAddressId));
                  const hasCoords = typeof chosen?.latitude === 'number' && typeof chosen?.longitude === 'number';
                  if (!hasCoords) {
                    return (
                      <div className="p-4 bg-amber-50 border border-amber-200 text-amber-900 rounded-xl text-xs space-y-2">
                        <div className="flex items-center gap-2 font-bold">
                          <AlertCircle size={16} className="text-amber-600 flex-shrink-0" />
                          <span>Coordinates Required for Delivery Tracking</span>
                        </div>
                        <p className="text-[11px] text-amber-800 leading-relaxed">
                          This saved address does not have GPS coordinates. StyleShop requires exact coordinates to dispatch your order and show tracking checkpoints. Please switch to <strong>"+ Add a New Address"</strong> below to pin your location on the map.
                        </p>
                        <button
                          type="button"
                          onClick={() => setSelectedAddressId('new')}
                          className="px-3 py-1.5 bg-amber-700 hover:bg-amber-800 text-white rounded-lg font-semibold text-xs transition cursor-pointer"
                        >
                          Switch to New Address with Map Pin
                        </button>
                      </div>
                    );
                  }
                  return (
                    <div className="p-3 bg-emerald-50 border border-emerald-200 rounded-xl text-xs flex items-center justify-between text-emerald-800">
                      <div className="flex items-center gap-2">
                        <CheckCircle2 size={15} className="text-emerald-600" />
                        <span className="font-semibold">Delivery GPS Pin Confirmed</span>
                      </div>
                      <span className="font-mono text-[11px] bg-white px-2 py-0.5 rounded border border-emerald-200">
                        {chosen.latitude.toFixed(4)}, {chosen.longitude.toFixed(4)}
                      </span>
                    </div>
                  );
                })()}

                {/* New Address Form & Interactive Location Picker (Fix 5, 6, 7) */}
                {selectedAddressId === 'new' && (
                  <div className="space-y-4 pt-2 border-t border-gray-100 animate-in fade-in">
                    <div className="flex items-center justify-between">
                      <h3 className="text-xs font-bold text-gray-800 uppercase tracking-wider">
                        New Address Details
                      </h3>
                      <span className="text-[11px] text-gray-400">All fields required</span>
                    </div>

                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
                      <div className="space-y-1 sm:col-span-2">
                        <label className="text-xs font-semibold text-gray-600">Recipient Full Name</label>
                        <input
                          type="text"
                          name="recipientName"
                          required
                          placeholder="e.g. Juan dela Cruz"
                          value={newAddress.recipientName}
                          onChange={handleNewAddressChange}
                          className="w-full px-3 py-2 text-xs bg-gray-50 border border-gray-300 rounded-lg placeholder-gray-400 focus:bg-white focus:outline-none focus:ring-1 focus:ring-gray-900"
                        />
                      </div>

                      <div className="space-y-1 sm:col-span-2">
                        <label className="text-xs font-semibold text-gray-600">Phone Number</label>
                        <input
                          type="tel"
                          name="phone"
                          required
                          placeholder="e.g. 0917 123 4567"
                          value={newAddress.phone}
                          onChange={handleNewAddressChange}
                          className="w-full px-3 py-2 text-xs bg-gray-50 border border-gray-300 rounded-lg placeholder-gray-400 focus:bg-white focus:outline-none focus:ring-1 focus:ring-gray-900"
                        />
                      </div>

                      <div className="space-y-1 sm:col-span-2">
                        <label className="text-xs font-semibold text-gray-600">Street Address</label>
                        <input
                          type="text"
                          name="street"
                          required
                          placeholder="e.g. Block 5 Lot 12 Sampaguita St"
                          value={newAddress.street}
                          onChange={handleNewAddressChange}
                          className="w-full px-3 py-2 text-xs bg-gray-50 border border-gray-300 rounded-lg placeholder-gray-400 focus:bg-white focus:outline-none focus:ring-1 focus:ring-gray-900"
                        />
                      </div>

                      <div className="space-y-1">
                        <label className="text-xs font-semibold text-gray-600">City / Municipality</label>
                        <input
                          type="text"
                          name="city"
                          required
                          placeholder="e.g. Davao City"
                          value={newAddress.city}
                          onChange={handleNewAddressChange}
                          className="w-full px-3 py-2 text-xs bg-gray-50 border border-gray-300 rounded-lg placeholder-gray-400 focus:bg-white focus:outline-none focus:ring-1 focus:ring-gray-900"
                        />
                      </div>

                      <div className="space-y-1">
                        <label className="text-xs font-semibold text-gray-600">Province / Region</label>
                        <input
                          type="text"
                          name="province"
                          required
                          placeholder="e.g. Davao del Sur"
                          value={newAddress.province}
                          onChange={handleNewAddressChange}
                          className="w-full px-3 py-2 text-xs bg-gray-50 border border-gray-300 rounded-lg placeholder-gray-400 focus:bg-white focus:outline-none focus:ring-1 focus:ring-gray-900"
                        />
                      </div>

                      <div className="space-y-1">
                        <label className="text-xs font-semibold text-gray-600">Postal Code</label>
                        <input
                          type="text"
                          name="postalCode"
                          required
                          placeholder="e.g. 8000"
                          value={newAddress.postalCode}
                          onChange={handleNewAddressChange}
                          className="w-full px-3 py-2 text-xs bg-gray-50 border border-gray-300 rounded-lg placeholder-gray-400 focus:bg-white focus:outline-none focus:ring-1 focus:ring-gray-900"
                        />
                      </div>

                      <div className="space-y-1">
                        <label className="text-xs font-semibold text-gray-600">Country</label>
                        <input
                          type="text"
                          disabled
                          value="Philippines"
                          className="w-full px-3 py-2 text-xs bg-gray-100 border border-gray-300 rounded-lg text-gray-600 cursor-not-allowed"
                        />
                      </div>
                    </div>

                    {/* Location Picker Map Section (Fix 7) */}
                    <div className="pt-3 space-y-2">
                      <div className="flex items-center justify-between">
                        <label className="text-xs font-bold text-gray-700 uppercase tracking-wider flex items-center gap-1.5">
                          <MapPin size={13} className="text-rose-600" />
                          <span>Pin Exact Delivery Location On Map</span>
                        </label>
                        <span className="text-[11px] text-gray-500">Required</span>
                      </div>
                      <p className="text-[11px] text-gray-500 leading-relaxed">
                        Search your city/barangay, click "Use My Current Location", or click anywhere on the street map to place your delivery pin.
                      </p>

                      <LocationPickerMap
                        latitude={newAddress.latitude}
                        longitude={newAddress.longitude}
                        onChange={(lat, lng) => {
                          setNewAddress(prev => ({
                            ...prev,
                            latitude: lat,
                            longitude: lng,
                          }));
                        }}
                        onAddressResolved={(resolved) => {
                          setNewAddress(prev => ({
                            ...prev,
                            street: prev.street || resolved.street,
                            city: prev.city || resolved.city,
                            province: prev.province || resolved.province,
                            postalCode: prev.postalCode || resolved.postalCode,
                          }));
                        }}
                        height="320px"
                      />
                    </div>
                  </div>
                )}
              </div>

              {/* Payment Method Card */}
              <div className="bg-white border border-gray-200 rounded-xl p-6 shadow-2xs space-y-4">
                <div className="flex items-center gap-2 text-sm font-bold text-gray-900">
                  <div className="w-5 h-5 bg-gray-900 text-white rounded-full flex items-center justify-center text-xs">
                    2
                  </div>
                  <h2>Payment Method</h2>
                </div>

                <div className="space-y-2.5 pt-2">
                  {[
                    { id: 'cod', label: 'Cash on Delivery (COD)', desc: 'Pay with cash upon package receipt', icon: Truck },
                    { id: 'card', label: 'Credit or Debit Card', desc: 'Secure payment via Visa, Mastercard, AMEX', icon: CreditCard },
                    { id: 'ewallet', label: 'E-Wallet / PayPal', desc: 'Instant checkout with digital balance', icon: Wallet },
                  ].map((m) => {
                    const Icon = m.icon;
                    const selected = payMethod === m.id;
                    return (
                      <label
                        key={m.id}
                        className={`flex items-start gap-3 p-3.5 border rounded-xl cursor-pointer transition-all ${
                          selected
                            ? 'border-gray-900 bg-gray-50/80 shadow-2xs'
                            : 'border-gray-200 hover:border-gray-300'
                        }`}
                      >
                        <input
                          type="radio"
                          name="paymentMethod"
                          value={m.id}
                          checked={selected}
                          onChange={() => setPayMethod(m.id)}
                          className="mt-1 text-gray-900 focus:ring-gray-900 w-4 h-4 border-gray-300"
                        />
                        <div className="flex-1">
                          <div className="flex items-center gap-1.5">
                            <Icon size={15} className="text-gray-700" />
                            <p className="text-xs font-bold text-gray-900">{m.label}</p>
                          </div>
                          <p className="text-[11px] text-gray-500 mt-0.5">{m.desc}</p>
                        </div>
                      </label>
                    );
                  })}
                </div>

                {/* Credit card inputs if card selected */}
                {payMethod === 'card' && (
                  <div className="mt-4 p-4 bg-gray-50 border border-gray-200 rounded-xl space-y-3 animate-in fade-in">
                    <div className="space-y-1">
                      <label className="text-xs font-semibold text-gray-700">Card Number</label>
                      <input
                        type="text"
                        value={cardData.number}
                        onChange={(e) => setCardData({ ...cardData, number: e.target.value })}
                        placeholder="1234 5678 9012 3456"
                        className="w-full px-3 py-2 text-xs bg-white border border-gray-300 rounded-lg focus:outline-none focus:ring-1 focus:ring-gray-900"
                      />
                    </div>
                    <div className="grid grid-cols-2 gap-3">
                      <div className="space-y-1">
                        <label className="text-xs font-semibold text-gray-700">Expiry Date</label>
                        <input
                          type="text"
                          value={cardData.expiry}
                          onChange={(e) => setCardData({ ...cardData, expiry: e.target.value })}
                          placeholder="MM / YY"
                          className="w-full px-3 py-2 text-xs bg-white border border-gray-300 rounded-lg focus:outline-none focus:ring-1 focus:ring-gray-900"
                        />
                      </div>
                      <div className="space-y-1">
                        <label className="text-xs font-semibold text-gray-700">CVV Security Code</label>
                        <input
                          type="password"
                          value={cardData.cvv}
                          onChange={(e) => setCardData({ ...cardData, cvv: e.target.value })}
                          placeholder="•••"
                          maxLength={4}
                          className="w-full px-3 py-2 text-xs bg-white border border-gray-300 rounded-lg focus:outline-none focus:ring-1 focus:ring-gray-900"
                        />
                      </div>
                    </div>
                  </div>
                )}
              </div>

            </div>

            {/* Right Column: Order Overview & Submit */}
            <div className="w-full lg:w-80 flex-shrink-0 space-y-4">
              <div className="bg-white border border-gray-200 rounded-xl p-5 shadow-2xs space-y-4">
                <h2 className="text-sm font-bold text-gray-900 uppercase tracking-wider">
                  Review Items ({cartItems.length})
                </h2>

                <div className="max-h-56 overflow-y-auto space-y-2.5 divide-y divide-gray-100 pr-1">
                  {cartItems.map((item) => (
                    <div key={`${item.id}-${item.size}`} className="pt-2 first:pt-0 flex items-center justify-between gap-3 text-xs">
                      <div className="flex items-center gap-2 truncate">
                        <img src={resolveImageUrl(item.image)} alt={item.name} className="w-9 h-11 object-cover rounded bg-gray-100 flex-shrink-0" />
                        <div className="truncate">
                          <p className="font-semibold text-gray-800 truncate">{item.name}</p>
                          <p className="text-[10px] text-gray-400">Qty: {item.quantity} · Size: {item.size}</p>
                        </div>
                      </div>
                      <span className="font-bold text-gray-800 flex-shrink-0">
                        ₱{(item.price * item.quantity).toFixed(2)}
                      </span>
                    </div>
                  ))}
                </div>

                <div className="border-t border-gray-200 pt-3 space-y-2 text-xs">
                  <div className="flex justify-between text-gray-600">
                    <span>Subtotal</span>
                    <span className="font-semibold text-gray-900">₱{subtotal.toFixed(2)}</span>
                  </div>
                  <div className="flex justify-between text-gray-600">
                    <span>Shipping</span>
                    <span className="font-semibold text-gray-900">
                      {shippingFee === 0 ? <span className="text-emerald-600 font-bold">FREE</span> : `₱${shippingFee.toFixed(2)}`}
                    </span>
                  </div>
                  {discountAmount > 0 && (
                    <div className="flex justify-between text-emerald-600 font-semibold">
                      <span>Discount</span>
                      <span>-₱{discountAmount.toFixed(2)}</span>
                    </div>
                  )}
                  <div className="border-t border-gray-200 pt-2 flex justify-between items-center text-sm font-black text-gray-900">
                    <span>Grand Total</span>
                    <span className="text-lg">₱{total.toFixed(2)}</span>
                  </div>
                </div>

                <button
                  type="submit"
                  disabled={placing}
                  className="w-full py-3.5 px-4 bg-gray-900 hover:bg-gray-800 text-white font-bold text-xs uppercase tracking-wider rounded-xl transition shadow-xs flex items-center justify-center gap-2 cursor-pointer disabled:opacity-50"
                >
                  {placing ? (
                    <span className="inline-block w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                  ) : (
                    <>
                      <span>Place Order (₱{total.toFixed(2)})</span>
                      <ArrowRight size={14} />
                    </>
                  )}
                </button>

                <p className="text-[11px] text-gray-400 text-center leading-normal">
                  By clicking Place Order you agree to our Terms of Sale and authorize processing through the API.
                </p>
              </div>
            </div>

          </div>
        </form>

      </div>

      <Footer />
    </div>
  );
}
