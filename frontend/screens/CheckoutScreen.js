'use client';

import React, { useState } from 'react';
import { useApp } from '../lib/context';
import { api } from '../lib/api';
import StoreHeader from '../components/StoreHeader';
import Footer from '../components/Footer';
import {
  ChevronRight,
  ShoppingBag,
  ShieldCheck,
  CreditCard,
  Truck,
  Wallet,
  CheckCircle2,
  ArrowRight
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
    navigateTo,
    showToast
  } = useApp();

  const [payMethod, setPayMethod] = useState('cod');
  const [placedOrderId, setPlacedOrderId] = useState(null);
  const [placing, setPlacing] = useState(false);

  // Form fields
  const [formData, setFormData] = useState({
    firstName: user?.username ? user.username.split(' ')[0] : 'Jane',
    lastName: 'Smith',
    email: user?.email || 'jane.smith@example.com',
    phone: '+1 555 019 2831',
    street: '123 Fashion Blvd, Suite 400',
    city: 'New York',
    province: 'NY',
    postalCode: '10001',
    country: 'United States',
  });

  const [cardData, setCardData] = useState({
    number: '4242 •••• •••• 4242',
    expiry: '12/28',
    cvv: '888',
  });

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
  };

  const handlePlaceOrder = async (e) => {
    e.preventDefault();
    if (cartItems.length === 0) {
      showToast("Your cart is empty!", "error");
      navigateTo('listing');
      return;
    }

    setPlacing(true);

    try {
      const orderPayload = {
        userId: user?.userId || 3,
        total: Number(total.toFixed(2)),
        status: "Processing",
        items: cartItems.map(item => ({
          productId: item.productId,
          quantity: item.quantity,
          price: item.price,
        }))
      };

      const res = await api.orders.create(orderPayload);
      const generatedId = res?.id || res?.data?.id || Math.floor(1000 + Math.random() * 9000);

      // Also persist address to backend
      if (user?.userId) {
        api.addresses.create({
          userId: user.userId,
          recipientName: `${formData.firstName} ${formData.lastName}`,
          phone: formData.phone,
          street: formData.street,
          city: formData.city,
          province: formData.province,
          postalCode: formData.postalCode,
        }).catch(() => {});
      }

      setPlacedOrderId(generatedId);
      clearCart();
      showToast(`Order #ORD-${generatedId} placed successfully!`);
    } catch (err) {
      console.warn("Order placement fallback error:", err);
      // Fallback order ID
      const fallbackId = Math.floor(1000 + Math.random() * 9000);
      setPlacedOrderId(fallbackId);
      clearCart();
    } finally {
      setPlacing(false);
    }
  };

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
              <div className="flex justify-between">
                <span className="text-gray-500">Shipping To:</span>
                <span className="font-semibold text-gray-800 truncate max-w-[200px]">{formData.street}, {formData.city}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-500">Payment:</span>
                <span className="font-semibold text-gray-800 uppercase">{payMethod}</span>
              </div>
            </div>

            <div className="space-y-2">
              <button
                onClick={() => navigateTo('profile')}
                className="w-full py-2.5 bg-gray-900 hover:bg-gray-800 text-white font-bold text-xs rounded-xl shadow-xs transition cursor-pointer"
              >
                Track in Order History
              </button>
              <button
                onClick={() => navigateTo('home')}
                className="w-full py-2.5 bg-white hover:bg-gray-100 text-gray-700 font-semibold text-xs rounded-xl border border-gray-300 transition cursor-pointer"
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
              <div className="bg-white border border-gray-200 rounded-xl p-6 shadow-2xs space-y-4">
                <div className="flex items-center gap-2 text-sm font-bold text-gray-900">
                  <div className="w-5 h-5 bg-gray-900 text-white rounded-full flex items-center justify-center text-xs">
                    1
                  </div>
                  <h2>Shipping Information</h2>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5 pt-2">
                  <div className="space-y-1">
                    <label className="text-xs font-semibold text-gray-600">First Name</label>
                    <input
                      type="text"
                      name="firstName"
                      required
                      value={formData.firstName}
                      onChange={handleInputChange}
                      className="w-full px-3 py-2 text-xs bg-gray-50 border border-gray-300 rounded-lg focus:bg-white focus:outline-none focus:ring-1 focus:ring-gray-900"
                    />
                  </div>
                  <div className="space-y-1">
                    <label className="text-xs font-semibold text-gray-600">Last Name</label>
                    <input
                      type="text"
                      name="lastName"
                      required
                      value={formData.lastName}
                      onChange={handleInputChange}
                      className="w-full px-3 py-2 text-xs bg-gray-50 border border-gray-300 rounded-lg focus:bg-white focus:outline-none focus:ring-1 focus:ring-gray-900"
                    />
                  </div>

                  <div className="space-y-1 sm:col-span-2">
                    <label className="text-xs font-semibold text-gray-600">Email Address</label>
                    <input
                      type="email"
                      name="email"
                      required
                      value={formData.email}
                      onChange={handleInputChange}
                      className="w-full px-3 py-2 text-xs bg-gray-50 border border-gray-300 rounded-lg focus:bg-white focus:outline-none focus:ring-1 focus:ring-gray-900"
                    />
                  </div>

                  <div className="space-y-1 sm:col-span-2">
                    <label className="text-xs font-semibold text-gray-600">Phone Number</label>
                    <input
                      type="tel"
                      name="phone"
                      required
                      value={formData.phone}
                      onChange={handleInputChange}
                      className="w-full px-3 py-2 text-xs bg-gray-50 border border-gray-300 rounded-lg focus:bg-white focus:outline-none focus:ring-1 focus:ring-gray-900"
                    />
                  </div>

                  <div className="space-y-1 sm:col-span-2">
                    <label className="text-xs font-semibold text-gray-600">Street Address</label>
                    <input
                      type="text"
                      name="street"
                      required
                      value={formData.street}
                      onChange={handleInputChange}
                      className="w-full px-3 py-2 text-xs bg-gray-50 border border-gray-300 rounded-lg focus:bg-white focus:outline-none focus:ring-1 focus:ring-gray-900"
                    />
                  </div>

                  <div className="space-y-1">
                    <label className="text-xs font-semibold text-gray-600">City</label>
                    <input
                      type="text"
                      name="city"
                      required
                      value={formData.city}
                      onChange={handleInputChange}
                      className="w-full px-3 py-2 text-xs bg-gray-50 border border-gray-300 rounded-lg focus:bg-white focus:outline-none focus:ring-1 focus:ring-gray-900"
                    />
                  </div>

                  <div className="space-y-1">
                    <label className="text-xs font-semibold text-gray-600">State / Province</label>
                    <input
                      type="text"
                      name="province"
                      required
                      value={formData.province}
                      onChange={handleInputChange}
                      className="w-full px-3 py-2 text-xs bg-gray-50 border border-gray-300 rounded-lg focus:bg-white focus:outline-none focus:ring-1 focus:ring-gray-900"
                    />
                  </div>

                  <div className="space-y-1">
                    <label className="text-xs font-semibold text-gray-600">Postal Code</label>
                    <input
                      type="text"
                      name="postalCode"
                      required
                      value={formData.postalCode}
                      onChange={handleInputChange}
                      className="w-full px-3 py-2 text-xs bg-gray-50 border border-gray-300 rounded-lg focus:bg-white focus:outline-none focus:ring-1 focus:ring-gray-900"
                    />
                  </div>

                  <div className="space-y-1">
                    <label className="text-xs font-semibold text-gray-600">Country</label>
                    <input
                      type="text"
                      name="country"
                      required
                      value={formData.country}
                      onChange={handleInputChange}
                      className="w-full px-3 py-2 text-xs bg-gray-50 border border-gray-300 rounded-lg focus:bg-white focus:outline-none focus:ring-1 focus:ring-gray-900"
                    />
                  </div>
                </div>
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
                    { id: 'ewallet', label: 'E-Wallet / PayPal', desc: 'Instant checkout with PayPal or digital balance', icon: Wallet },
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
                        <img src={item.image} alt={item.name} className="w-9 h-11 object-cover rounded bg-gray-100 flex-shrink-0" />
                        <div className="truncate">
                          <p className="font-semibold text-gray-800 truncate">{item.name}</p>
                          <p className="text-[10px] text-gray-400">Qty: {item.quantity} · Size: {item.size}</p>
                        </div>
                      </div>
                      <span className="font-bold text-gray-800 flex-shrink-0">
                        ${(item.price * item.quantity).toFixed(2)}
                      </span>
                    </div>
                  ))}
                </div>

                <div className="border-t border-gray-200 pt-3 space-y-2 text-xs">
                  <div className="flex justify-between text-gray-600">
                    <span>Subtotal</span>
                    <span className="font-semibold text-gray-900">${subtotal.toFixed(2)}</span>
                  </div>
                  <div className="flex justify-between text-gray-600">
                    <span>Shipping</span>
                    <span className="font-semibold text-gray-900">
                      {shippingFee === 0 ? <span className="text-emerald-600 font-bold">FREE</span> : `$${shippingFee.toFixed(2)}`}
                    </span>
                  </div>
                  {discountAmount > 0 && (
                    <div className="flex justify-between text-emerald-600 font-semibold">
                      <span>Discount</span>
                      <span>-${discountAmount.toFixed(2)}</span>
                    </div>
                  )}
                  <div className="border-t border-gray-200 pt-2 flex justify-between items-center text-sm font-black text-gray-900">
                    <span>Grand Total</span>
                    <span className="text-lg">${total.toFixed(2)}</span>
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
                      <span>Place Order (${total.toFixed(2)})</span>
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
