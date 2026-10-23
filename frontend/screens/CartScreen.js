'use client';

import React, { useState } from 'react';
import { useApp } from '../lib/context';
import { resolveImageUrl } from '../lib/api';
import StoreHeader from '../components/StoreHeader';
import Footer from '../components/Footer';
import { Trash2, ChevronLeft, ArrowRight, ShieldCheck, Tag, ShoppingBag, X } from 'lucide-react';

export default function CartScreen() {
  const {
    cartItems,
    updateCartQuantity,
    removeFromCart,
    clearCart,
    subtotal,
    discountAmount,
    shippingFee,
    total,
    appliedCoupon,
    applyCouponCode,
    removeCoupon,
    navigateTo,
    isLoggedIn,
    setRedirectAfterLogin,
    showToast,
  } = useApp();

  const [couponInput, setCouponInput] = useState('');
  const [couponError, setCouponError] = useState('');
  const [applying, setApplying] = useState(false);

  const handleProceedToCheckout = () => {
    if (!isLoggedIn) {
      setRedirectAfterLogin('checkout');
      showToast("Please log in to continue", "info");
      navigateTo('login');
    } else {
      navigateTo('checkout');
    }
  };

  const handleApplyCoupon = async (e) => {
    e.preventDefault();
    if (!couponInput.trim()) return;

    setApplying(true);
    setCouponError('');
    const res = await applyCouponCode(couponInput.trim());
    if (!res.success) {
      setCouponError(res.message);
    } else {
      setCouponInput('');
    }
    setApplying(false);
  };

  if (cartItems.length === 0) {
    return (
      <div className="min-h-screen bg-gray-50 flex flex-col">
        <StoreHeader />
        <div className="flex-1 max-w-4xl mx-auto px-6 py-16 flex flex-col items-center justify-center text-center">
          <div className="w-16 h-16 bg-gray-100 rounded-full flex items-center justify-center mb-4 text-gray-400">
            <ShoppingBag size={28} />
          </div>
          <h1 className="text-xl font-bold text-gray-900 mb-2">Your Shopping Cart is Empty</h1>
          <p className="text-sm text-gray-500 max-w-sm mb-6">
            Looks like you haven't added any pieces to your cart yet. Explore our curated collections to get started.
          </p>
          <button
            onClick={() => navigateTo('listing')}
            className="px-6 py-2.5 bg-gray-900 hover:bg-gray-800 text-white font-semibold text-xs rounded-xl shadow-xs transition cursor-pointer"
          >
            Explore Catalog
          </button>
        </div>
        <Footer />
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 flex flex-col">
      <StoreHeader />

      <div className="max-w-6xl mx-auto px-4 sm:px-6 py-8 flex-1 w-full">
        
        <h1 className="text-xl font-black text-gray-900 mb-6 tracking-tight">
          Your Cart ({cartItems.length} {cartItems.length === 1 ? 'item' : 'items'})
        </h1>

        <div className="flex flex-col lg:flex-row gap-8">
          
          {/* Cart Items Table */}
          <div className="flex-1 space-y-4">
            <div className="bg-white border border-gray-200 rounded-xl overflow-hidden shadow-2xs">
              <table className="w-full text-left text-sm">
                <thead>
                  <tr className="border-b border-gray-200 bg-gray-50/80 text-[11px] font-bold text-gray-500 uppercase tracking-wider">
                    <th className="px-5 py-3.5">Product</th>
                    <th className="px-4 py-3.5">Size</th>
                    <th className="px-4 py-3.5">Quantity</th>
                    <th className="px-4 py-3.5">Price</th>
                    <th className="px-4 py-3.5">Total</th>
                    <th className="px-4 py-3.5 text-right"></th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {cartItems.map((item) => (
                    <tr key={`${item.id}-${item.size}`} className="hover:bg-gray-50/60 transition-colors">
                      {/* Product Thumbnail & Title */}
                      <td className="px-5 py-4">
                        <div className="flex items-center gap-3.5">
                          <img
                            src={resolveImageUrl(item.image)}
                            alt={item.name}
                            className="w-14 h-16 object-cover rounded-lg border border-gray-200 bg-gray-100 flex-shrink-0"
                          />
                          <div>
                            <p
                              onClick={() => navigateTo('details', { productId: item.productId })}
                              className="font-bold text-gray-900 hover:text-indigo-600 transition cursor-pointer text-sm"
                            >
                              {item.name}
                            </p>
                            <p className="text-[11px] text-gray-400 mt-0.5 font-mono">
                              SKU: WRF-{1000 + item.productId}
                            </p>
                          </div>
                        </div>
                      </td>

                      {/* Size */}
                      <td className="px-4 py-4">
                        <span className="px-2 py-0.5 rounded text-xs font-semibold bg-gray-100 border border-gray-200 text-gray-700">
                          {item.size || 'M'}
                        </span>
                      </td>

                      {/* Quantity Stepper */}
                      <td className="px-4 py-4">
                        <div className="flex items-center border border-gray-300 rounded-lg overflow-hidden w-fit bg-white">
                          <button
                            onClick={() => updateCartQuantity(item.id, item.quantity - 1)}
                            className="px-2.5 py-1 text-xs text-gray-600 hover:bg-gray-100 transition cursor-pointer"
                          >
                            −
                          </button>
                          <span className="px-2.5 py-1 text-xs font-bold text-gray-800 border-x border-gray-200 min-w-[28px] text-center select-none">
                            {item.quantity}
                          </span>
                          <button
                            onClick={() => updateCartQuantity(item.id, item.quantity + 1)}
                            className="px-2.5 py-1 text-xs text-gray-600 hover:bg-gray-100 transition cursor-pointer"
                          >
                            +
                          </button>
                        </div>
                      </td>

                      {/* Price */}
                      <td className="px-4 py-4 text-xs font-semibold text-gray-600">
                        ₱{Number(item.price).toFixed(2)}
                      </td>

                      {/* Line Total */}
                      <td className="px-4 py-4 text-sm font-black text-gray-900">
                        ₱{(Number(item.price) * item.quantity).toFixed(2)}
                      </td>

                      {/* Remove Button */}
                      <td className="px-4 py-4 text-right">
                        <button
                          onClick={() => removeFromCart(item.id)}
                          className="p-1.5 text-gray-400 hover:text-red-600 transition cursor-pointer rounded"
                          title="Remove item"
                        >
                          <Trash2 size={16} />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Bottom Actions */}
            <div className="flex items-center justify-between pt-2">
              <button
                onClick={() => navigateTo('listing')}
                className="flex items-center gap-1.5 px-4 py-2 bg-white border border-gray-300 hover:border-gray-400 text-gray-700 rounded-xl text-xs font-semibold transition cursor-pointer"
              >
                <ChevronLeft size={14} />
                <span>Continue Shopping</span>
              </button>
              <button
                onClick={clearCart}
                className="text-xs text-gray-400 hover:text-red-600 transition cursor-pointer"
              >
                Empty Cart
              </button>
            </div>
          </div>

          {/* Right Column: Order Summary */}
          <div className="w-full lg:w-80 flex-shrink-0 space-y-4">
            <div className="bg-white border border-gray-200 rounded-xl p-5 shadow-2xs space-y-4">
              <h2 className="text-sm font-bold text-gray-900 uppercase tracking-wider">
                Order Summary
              </h2>

              <div className="space-y-2.5 text-xs border-t border-gray-100 pt-3">
                <div className="flex justify-between text-gray-600">
                  <span>Subtotal</span>
                  <span className="font-semibold text-gray-900">₱{subtotal.toFixed(2)}</span>
                </div>

                <div className="flex justify-between text-gray-600">
                  <span>Estimated Shipping</span>
                  <span className="font-semibold text-gray-900">
                    {shippingFee === 0 ? <span className="text-emerald-600 font-bold">FREE</span> : `₱${shippingFee.toFixed(2)}`}
                  </span>
                </div>

                {appliedCoupon && (
                  <div className="flex justify-between text-emerald-600 font-semibold bg-emerald-50 px-2 py-1.5 rounded-lg border border-emerald-200">
                    <span className="flex items-center gap-1">
                      <Tag size={12} />
                      <span>Coupon: {appliedCoupon.code} ({appliedCoupon.discount}%)</span>
                    </span>
                    <div className="flex items-center gap-1">
                      <span>-₱{discountAmount.toFixed(2)}</span>
                      <button
                        onClick={removeCoupon}
                        className="text-emerald-700 hover:text-red-600 transition ml-1"
                        title="Remove coupon"
                      >
                        <X size={12} />
                      </button>
                    </div>
                  </div>
                )}
              </div>

              {/* Total Row */}
              <div className="border-t border-gray-200 pt-3 flex justify-between items-center text-sm font-black text-gray-900">
                <span>Total Amount</span>
                <span className="text-lg">₱{total.toFixed(2)}</span>
              </div>

              {/* Promo Code Input */}
              <form onSubmit={handleApplyCoupon} className="pt-2">
                <div className="flex gap-1.5">
                  <input
                    type="text"
                    value={couponInput}
                    onChange={(e) => setCouponInput(e.target.value.toUpperCase())}
                    placeholder="Coupon code (e.g. WELCOME20)"
                    className="flex-1 px-3 py-2 text-xs bg-gray-50 border border-gray-300 rounded-lg placeholder-gray-400 focus:bg-white focus:outline-none focus:ring-1 focus:ring-gray-900"
                  />
                  <button
                    type="submit"
                    disabled={applying}
                    className="px-3 py-2 bg-gray-100 hover:bg-gray-200 text-gray-800 text-xs font-semibold rounded-lg border border-gray-300 transition cursor-pointer"
                  >
                    {applying ? "..." : "Apply"}
                  </button>
                </div>
                {couponError && (
                  <p className="text-[11px] text-red-600 mt-1">{couponError}</p>
                )}
              </form>

              {/* Proceed to Checkout CTA */}
              <button
                onClick={handleProceedToCheckout}
                className="w-full py-3 px-4 bg-gray-900 hover:bg-gray-800 text-white font-bold text-xs uppercase tracking-wider rounded-xl transition-colors shadow-xs flex items-center justify-center gap-2 cursor-pointer mt-2"
              >
                <span>Proceed to Checkout</span>
                <ArrowRight size={14} />
              </button>

              <div className="text-[11px] text-gray-400 text-center flex items-center justify-center gap-1.5 pt-1">
                <ShieldCheck size={14} className="text-gray-500" />
                <span>Encrypted 256-Bit SSL Checkout</span>
              </div>
            </div>

            {/* Benefit reassurance box */}
            <div className="bg-white border border-gray-200 rounded-xl p-3.5 text-center text-xs text-gray-500 shadow-2xs">
              <p className="font-semibold text-gray-700 mb-0.5">7-Day Happiness Guarantee</p>
              <p className="text-[11px]">Free prepaid returns & exchanges on all eligible garments.</p>
            </div>
          </div>

        </div>

      </div>

      <Footer />
    </div>
  );
}
