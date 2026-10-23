'use client';

import React, { useEffect, useState } from 'react';
import { useApp } from '../lib/context';
import { api, resolveImageUrl } from '../lib/api';
import StoreHeader from '../components/StoreHeader';
import Footer from '../components/Footer';
import { ArrowRight, Tag, Sparkles, TrendingUp, Star, AlertCircle } from 'lucide-react';

export default function HomeScreen() {
  const { navigateTo, applyCouponCode } = useApp();
  const [topProducts, setTopProducts] = useState([]);
  const [topLoading, setTopLoading] = useState(true);
  const [topError, setTopError] = useState(null);

  const [allProducts, setAllProducts] = useState([]);
  const [allLoading, setAllLoading] = useState(true);

  useEffect(() => {
    let isMounted = true;

    async function loadData() {
      // Fetch top products
      setTopLoading(true);
      setTopError(null);
      try {
        const res = await api.products.getTop(8);
        if (isMounted) {
          if (res.success && Array.isArray(res.data)) {
            setTopProducts(res.data);
          } else {
            setTopError(res.error || 'Could not load products.');
            setTopProducts([]);
          }
        }
      } catch (err) {
        if (isMounted) {
          setTopError('Could not load products.');
          setTopProducts([]);
        }
      } finally {
        if (isMounted) setTopLoading(false);
      }

      // Fetch all products for New Arrivals
      try {
        const all = await api.products.getAll();
        if (isMounted && Array.isArray(all)) {
          setAllProducts(all);
        }
      } catch (err) {
        console.warn("Could not load products for new arrivals", err);
      } finally {
        if (isMounted) setAllLoading(false);
      }
    }

    loadData();
    return () => { isMounted = false; };
  }, []);

  // New arrivals: 8 newest products (id DESC) excluding products already in Top Products
  const topIds = new Set(topProducts.map((p) => p.id));
  const newArrivals = [...allProducts]
    .filter((p) => !topIds.has(p.id))
    .sort((a, b) => b.id - a.id)
    .slice(0, 8);

  return (
    <div className="min-h-screen bg-gray-50 flex flex-col">
      <StoreHeader />

      {/* Hero Section */}
      <section className="bg-gray-200 border-b border-gray-300">
        <div className="max-w-6xl mx-auto px-6 py-14 md:py-20 flex flex-col md:flex-row items-center gap-10">
          
          <div className="flex-1 max-w-xl">
            <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold uppercase tracking-wider bg-white border border-gray-300 text-gray-700 mb-4 shadow-2xs">
              <Sparkles size={13} className="text-gray-900" />
              New Collection 2025
            </span>
            <h1 className="text-3xl sm:text-4xl lg:text-5xl font-extrabold text-gray-900 mb-4 tracking-tight leading-tight">
              Dress for Every Occasion
            </h1>
            <p className="text-sm sm:text-base text-gray-600 mb-8 leading-relaxed">
              Discover curated styles for every season. Quality fabrics, timeless cuts, and modern silhouettes designed to elevate your everyday rotation.
            </p>
            <div className="flex flex-wrap items-center gap-3">
              <button
                onClick={() => navigateTo('listing')}
                className="px-6 py-3 bg-gray-900 hover:bg-gray-800 text-white font-semibold text-sm rounded-lg transition-colors shadow-xs flex items-center gap-2 cursor-pointer"
              >
                <span>Shop Now</span>
                <ArrowRight size={15} />
              </button>
              <button
                onClick={() => navigateTo('listing', { category: 'Tops' })}
                className="px-6 py-3 bg-white hover:bg-gray-100 text-gray-800 font-semibold text-sm rounded-lg border border-gray-300 transition-colors shadow-2xs cursor-pointer"
              >
                View Lookbook
              </button>
            </div>
          </div>

          {/* Hero Banner Visual Card */}
          <div className="w-full md:w-80 lg:w-96 flex-shrink-0">
            <div className="relative rounded-2xl overflow-hidden border border-gray-300 bg-gray-300 shadow-md group">
              <img
                src="https://images.unsplash.com/photo-1490481651871-ab68de25d43d?auto=format&fit=crop&w=800&q=80"
                alt="Season Lookbook"
                className="w-full h-80 object-cover group-hover:scale-103 transition duration-500"
              />
              <div className="absolute inset-0 bg-gradient-to-t from-gray-950/80 via-transparent to-transparent flex flex-col justify-end p-5 text-white">
                <span className="text-[11px] font-bold uppercase tracking-wider text-gray-300">Curated Capsule</span>
                <p className="text-base font-bold">Autumn Essentials</p>
                <p className="text-xs text-gray-300 mt-0.5">Explore 100+ timeless everyday staples</p>
              </div>
            </div>
          </div>

        </div>
      </section>

      {/* Main Content Area */}
      <main className="max-w-6xl mx-auto px-6 py-12 flex-1 space-y-14 w-full">
        
        {/* Section 1: Top Products */}
        <section>
          <div className="flex items-center gap-4 mb-6">
            <h2 className="text-sm font-bold text-gray-900 uppercase tracking-wider flex items-center gap-2">
              <TrendingUp size={16} className="text-gray-700" />
              Top Products
            </h2>
            <div className="flex-1 h-px bg-gray-200" />
            <button
              onClick={() => navigateTo('listing')}
              className="text-xs font-semibold text-gray-600 hover:text-gray-900 underline cursor-pointer"
            >
              See all
            </button>
          </div>

          {/* Loading Skeleton */}
          {topLoading && (
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 sm:gap-5">
              {[...Array(8)].map((_, i) => (
                <div key={i} className="bg-white border border-gray-200 rounded-xl overflow-hidden animate-pulse">
                  <div className="h-48 sm:h-52 bg-gray-200" />
                  <div className="p-4 space-y-2.5">
                    <div className="h-4 bg-gray-200 rounded w-3/4" />
                    <div className="h-4 bg-gray-200 rounded w-1/3" />
                    <div className="h-8 bg-gray-100 rounded mt-3" />
                  </div>
                </div>
              ))}
            </div>
          )}

          {/* Error State */}
          {!topLoading && topError && (
            <div className="bg-red-50 border border-red-200 rounded-xl p-8 text-center text-red-700">
              <AlertCircle size={24} className="mx-auto mb-2 text-red-500" />
              <p className="font-semibold text-sm">Could not load products.</p>
            </div>
          )}

          {/* Empty State */}
          {!topLoading && !topError && topProducts.length === 0 && (
            <div className="bg-white border border-dashed border-gray-300 rounded-xl p-10 text-center text-gray-500 text-sm">
              Top products will appear here soon
            </div>
          )}

          {/* Loaded Products Grid */}
          {!topLoading && !topError && topProducts.length > 0 && (
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 sm:gap-5">
              {topProducts.map((p, index) => {
                const isBestSeller = index < 3 && (p.soldCount || 0) > 0;
                const isSoldOut = p.stock === 0;

                return (
                  <div
                    key={p.id}
                    className="bg-white border border-gray-200 rounded-xl overflow-hidden hover:shadow-md transition-shadow group flex flex-col"
                  >
                    <div
                      onClick={() => navigateTo('details', { productId: p.id })}
                      className="h-48 sm:h-52 bg-gray-100 overflow-hidden relative cursor-pointer"
                    >
                      <img
                        src={resolveImageUrl(p.image) || "https://images.unsplash.com/photo-1521572267360-ee0c2909d518?auto=format&fit=crop&w=600&q=80"}
                        alt={p.name}
                        className={`w-full h-full object-cover group-hover:scale-105 transition-transform duration-300 ${isSoldOut ? 'opacity-60 grayscale-[30%]' : ''}`}
                      />

                      {/* Top Badges */}
                      <div className="absolute top-2.5 left-2.5 flex flex-col gap-1 items-start">
                        <span className="px-2 py-0.5 text-[10px] font-bold uppercase bg-white/90 backdrop-blur-xs text-gray-800 rounded border border-gray-200 shadow-2xs">
                          {p.category}
                        </span>
                        {isBestSeller && (
                          <span className="px-2 py-0.5 text-[10px] font-extrabold uppercase bg-amber-500 text-white rounded shadow-2xs tracking-wide">
                            Best Seller
                          </span>
                        )}
                      </div>

                      {/* Sold Out Badge */}
                      {isSoldOut && (
                        <span className="absolute top-2.5 right-2.5 px-2 py-0.5 text-[10px] font-extrabold uppercase bg-red-600 text-white rounded shadow-2xs">
                          Sold out
                        </span>
                      )}
                    </div>

                    <div className="p-4 flex-1 flex flex-col justify-between">
                      <div>
                        <h3
                          onClick={() => navigateTo('details', { productId: p.id })}
                          className="text-sm font-bold text-gray-900 hover:text-indigo-600 transition cursor-pointer truncate"
                          title={p.name}
                        >
                          {p.name}
                        </h3>

                        <div className="flex items-baseline justify-between mt-1">
                          <p className="text-sm font-bold text-gray-900">
                            ₱{Number(p.price).toFixed(2)}
                          </p>
                          {p.soldCount > 0 && (
                            <span className="text-[11px] text-gray-500 font-medium">
                              {p.soldCount} sold
                            </span>
                          )}
                        </div>

                        {/* Star Rating - Only shown if reviewCount > 0 */}
                        {p.reviewCount > 0 && (
                          <div className="flex items-center gap-1 mt-1.5">
                            <Star size={13} className="fill-amber-400 text-amber-400 flex-shrink-0" />
                            <span className="text-xs font-bold text-gray-800">
                              {Number(p.averageRating).toFixed(1)}
                            </span>
                            <span className="text-[11px] text-gray-400">
                              ({p.reviewCount})
                            </span>
                          </div>
                        )}
                      </div>

                      <div className="pt-3 mt-3 border-t border-gray-100">
                        <button
                          onClick={() => navigateTo('details', { productId: p.id })}
                          className="w-full py-1.5 px-3 text-xs font-semibold text-gray-700 bg-gray-50 hover:bg-gray-100 border border-gray-300 rounded-lg transition text-center cursor-pointer"
                        >
                          View Details
                        </button>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </section>

        {/* Section 2: New Arrivals */}
        <section>
          <div className="flex items-center gap-4 mb-6">
            <h2 className="text-sm font-bold text-gray-900 uppercase tracking-wider flex items-center gap-2">
              <Sparkles size={16} className="text-gray-700" />
              New Arrivals
            </h2>
            <div className="flex-1 h-px bg-gray-200" />
            <button
              onClick={() => navigateTo('listing')}
              className="text-xs font-semibold text-gray-600 hover:text-gray-900 underline cursor-pointer"
            >
              Browse Catalog
            </button>
          </div>

          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            {newArrivals.map((p) => (
              <div
                key={p.id}
                onClick={() => navigateTo('details', { productId: p.id })}
                className="bg-white border border-gray-200 rounded-xl overflow-hidden hover:shadow-md transition-shadow cursor-pointer group"
              >
                <div className="h-44 bg-gray-100 overflow-hidden relative">
                  <img
                    src={resolveImageUrl(p.image) || "https://images.unsplash.com/photo-1521572267360-ee0c2909d518?auto=format&fit=crop&w=600&q=80"}
                    alt={p.name}
                    className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
                  />
                  <span className="absolute bottom-2 left-2 text-[10px] font-medium bg-black/60 text-white px-1.5 py-0.5 rounded backdrop-blur-xs">
                    {p.category}
                  </span>
                  {p.stock === 0 && (
                    <span className="absolute top-2 right-2 px-1.5 py-0.5 text-[9px] font-bold uppercase bg-red-600 text-white rounded">
                      Sold out
                    </span>
                  )}
                </div>
                <div className="p-3">
                  <p className="text-xs font-semibold text-gray-800 truncate">{p.name}</p>
                  <p className="text-xs font-bold text-gray-900 mt-0.5">₱{Number(p.price).toFixed(2)}</p>
                </div>
              </div>
            ))}
          </div>
        </section>

        {/* Section 3: Promo Banner with coupon code */}
        <section className="bg-gradient-to-r from-gray-100 to-gray-200 border border-gray-300 rounded-2xl p-6 sm:p-8 flex flex-col md:flex-row items-center justify-between gap-6 shadow-xs">
          <div className="space-y-1.5 text-center md:text-left">
            <span className="inline-flex items-center gap-1 text-[11px] font-bold text-gray-600 uppercase tracking-widest bg-gray-300/80 px-2.5 py-0.5 rounded-full">
              <Tag size={12} />
              Limited Time Special Offer
            </span>
            <p className="text-xl sm:text-2xl font-black text-gray-900">
              Get 20% Off Your Entire Order
            </p>
            <p className="text-sm text-gray-600">
              Use promo code <span className="font-mono font-bold text-gray-900 bg-white px-2 py-0.5 rounded border border-gray-300">WELCOME20</span> at checkout to unlock savings.
            </p>
          </div>

          <div className="flex items-center gap-3">
            <button
              onClick={() => {
                applyCouponCode('WELCOME20');
                navigateTo('cart');
              }}
              className="px-5 py-2.5 bg-gray-900 hover:bg-gray-800 text-white text-xs font-bold rounded-lg transition-colors shadow-xs cursor-pointer"
            >
              Apply to Cart
            </button>
            <button
              onClick={() => navigateTo('listing')}
              className="px-5 py-2.5 bg-white hover:bg-gray-50 text-gray-800 text-xs font-bold rounded-lg border border-gray-300 transition-colors cursor-pointer"
            >
              Shop Collection
            </button>
          </div>
        </section>

      </main>

      <Footer />
    </div>
  );
}
