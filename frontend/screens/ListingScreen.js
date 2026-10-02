'use client';

import React, { useState, useEffect, useMemo } from 'react';
import { useApp } from '../lib/context';
import { api, FALLBACK_PRODUCTS } from '../lib/api';
import StoreHeader from '../components/StoreHeader';
import Footer from '../components/Footer';
import { ChevronLeft, ChevronRight, SlidersHorizontal, X, ArrowUpDown } from 'lucide-react';

const CATEGORIES = ['All', 'Tops', 'Bottoms', 'Shoes', 'Accessories'];
const SIZES = ['XS', 'S', 'M', 'L', 'XL'];
const COLORS = [
  { name: 'Light Gray', hex: '#d1d5db' },
  { name: 'Gray', hex: '#9ca3af' },
  { name: 'Slate', hex: '#6b7280' },
  { name: 'Charcoal', hex: '#374151' },
  { name: 'Black', hex: '#111827' },
  { name: 'Cream', hex: '#f3f4f6' },
];

export default function ListingScreen() {
  const { navigateTo, selectedCategory, setSelectedCategory, searchQuery, setSearchQuery } = useApp();

  const [products, setProducts] = useState(FALLBACK_PRODUCTS);
  const [loading, setLoading] = useState(true);
  
  // Filter states
  const [activeCategory, setActiveCategory] = useState(selectedCategory || 'All');
  const [activeSizes, setActiveSizes] = useState([]);
  const [activeColor, setActiveColor] = useState(null);
  const [priceMax, setPriceMax] = useState(200);
  const [sortBy, setSortBy] = useState('newest');
  const [currentPage, setCurrentPage] = useState(1);
  const [mobileFilterOpen, setMobileFilterOpen] = useState(false);

  // Sync category with global state
  useEffect(() => {
    if (selectedCategory) {
      setActiveCategory(selectedCategory);
    }
  }, [selectedCategory]);

  // Load products from API
  useEffect(() => {
    let mounted = true;
    async function fetchProducts() {
      setLoading(true);
      try {
        let data;
        if (searchQuery && searchQuery.trim()) {
          data = await api.products.search(searchQuery.trim());
        } else if (activeCategory && activeCategory !== 'All') {
          data = await api.products.getByCategory(activeCategory);
        } else {
          data = await api.products.getAll();
        }

        if (mounted && Array.isArray(data)) {
          setProducts(data);
        }
      } catch (err) {
        console.warn("Listing fetch error:", err);
      } finally {
        if (mounted) setLoading(false);
      }
    }
    fetchProducts();
    return () => { mounted = false; };
  }, [activeCategory, searchQuery]);

  // Client filtering & sorting
  const filteredProducts = useMemo(() => {
    return products.filter((p) => {
      // Category filter (if not handled by backend)
      if (activeCategory !== 'All' && p.category.toLowerCase() !== activeCategory.toLowerCase()) {
        return false;
      }
      // Price filter
      if (p.price > priceMax) {
        return false;
      }
      return true;
    }).sort((a, b) => {
      if (sortBy === 'price-low') return a.price - b.price;
      if (sortBy === 'price-high') return b.price - a.price;
      if (sortBy === 'name') return a.name.localeCompare(b.name);
      return b.id - a.id; // newest first
    });
  }, [products, activeCategory, priceMax, sortBy]);

  const toggleSize = (s) => {
    setActiveSizes(prev => prev.includes(s) ? prev.filter(x => x !== s) : [...prev, s]);
  };

  const clearAllFilters = () => {
    setActiveCategory('All');
    setSelectedCategory('All');
    setActiveSizes([]);
    setActiveColor(null);
    setPriceMax(200);
    setSearchQuery('');
    setSortBy('newest');
  };

  // Pagination slice
  const pageSize = 9;
  const totalPages = Math.ceil(filteredProducts.length / pageSize) || 1;
  const paginatedList = filteredProducts.slice((currentPage - 1) * pageSize, currentPage * pageSize);

  return (
    <div className="min-h-screen bg-gray-50 flex flex-col">
      <StoreHeader />

      <div className="max-w-6xl mx-auto px-4 sm:px-6 py-8 flex-1 w-full">
        
        {/* Mobile Filter Toggle */}
        <div className="lg:hidden flex items-center justify-between mb-4 pb-3 border-b border-gray-200">
          <button
            onClick={() => setMobileFilterOpen(!mobileFilterOpen)}
            className="flex items-center gap-2 px-3 py-1.5 bg-white border border-gray-300 rounded-lg text-xs font-semibold text-gray-700"
          >
            <SlidersHorizontal size={14} />
            <span>Filters ({[activeCategory !== 'All', activeSizes.length > 0, priceMax < 200].filter(Boolean).length})</span>
          </button>
          <span className="text-xs text-gray-500 font-medium">{filteredProducts.length} items</span>
        </div>

        <div className="flex gap-8">
          
          {/* Sidebar Filters */}
          <aside className={`w-60 flex-shrink-0 space-y-6 ${mobileFilterOpen ? 'block' : 'hidden lg:block'}`}>
            <div className="bg-white p-5 rounded-xl border border-gray-200 space-y-6 shadow-2xs">
              
              {/* Category Filter */}
              <div>
                <p className="text-xs font-bold text-gray-900 uppercase tracking-wider mb-3">
                  Categories
                </p>
                <div className="space-y-1.5">
                  {CATEGORIES.map((cat) => (
                    <label
                      key={cat}
                      className="flex items-center gap-2.5 text-sm text-gray-600 hover:text-gray-900 cursor-pointer select-none py-0.5"
                    >
                      <input
                        type="radio"
                        name="category"
                        checked={activeCategory === cat}
                        onChange={() => {
                          setActiveCategory(cat);
                          setSelectedCategory(cat);
                        }}
                        className="rounded text-gray-900 focus:ring-gray-900 w-4 h-4 border-gray-300"
                      />
                      <span className={activeCategory === cat ? 'font-semibold text-gray-900' : ''}>
                        {cat}
                      </span>
                    </label>
                  ))}
                </div>
              </div>

              {/* Size Filter */}
              <div className="border-t border-gray-200 pt-5">
                <p className="text-xs font-bold text-gray-900 uppercase tracking-wider mb-3">
                  Sizes
                </p>
                <div className="flex flex-wrap gap-1.5">
                  {SIZES.map((size) => {
                    const active = activeSizes.includes(size);
                    return (
                      <button
                        key={size}
                        type="button"
                        onClick={() => toggleSize(size)}
                        className={`w-9 h-8 text-xs font-semibold rounded-md border transition-all cursor-pointer ${
                          active
                            ? 'bg-gray-900 text-white border-gray-900'
                            : 'bg-white text-gray-700 border-gray-300 hover:border-gray-500'
                        }`}
                      >
                        {size}
                      </button>
                    );
                  })}
                </div>
              </div>

              {/* Color Filter */}
              <div className="border-t border-gray-200 pt-5">
                <p className="text-xs font-bold text-gray-900 uppercase tracking-wider mb-3">
                  Colors
                </p>
                <div className="flex flex-wrap gap-2.5">
                  {COLORS.map((col) => {
                    const isSelected = activeColor === col.hex;
                    return (
                      <button
                        key={col.hex}
                        type="button"
                        onClick={() => setActiveColor(isSelected ? null : col.hex)}
                        title={col.name}
                        style={{ backgroundColor: col.hex }}
                        className={`w-7 h-7 rounded-full border-2 transition-all cursor-pointer ${
                          isSelected
                            ? 'border-gray-900 ring-2 ring-gray-400 scale-110'
                            : 'border-white ring-1 ring-gray-300 hover:ring-gray-500'
                        }`}
                      />
                    );
                  })}
                </div>
              </div>

              {/* Price Range */}
              <div className="border-t border-gray-200 pt-5">
                <div className="flex justify-between items-center mb-2">
                  <p className="text-xs font-bold text-gray-900 uppercase tracking-wider">
                    Max Price
                  </p>
                  <span className="text-xs font-bold text-gray-800">${priceMax}</span>
                </div>
                <input
                  type="range"
                  min="20"
                  max="250"
                  step="5"
                  value={priceMax}
                  onChange={(e) => setPriceMax(Number(e.target.value))}
                  className="w-full accent-gray-900 cursor-pointer"
                />
                <div className="flex justify-between text-[11px] text-gray-400 mt-1">
                  <span>$20</span>
                  <span>$250</span>
                </div>
              </div>

              {/* Actions */}
              <div className="pt-2 space-y-2">
                <button
                  type="button"
                  onClick={clearAllFilters}
                  className="w-full py-2 px-3 text-xs font-semibold text-gray-500 hover:text-gray-900 hover:bg-gray-100 rounded-lg transition"
                >
                  Clear All Filters
                </button>
              </div>

            </div>
          </aside>

          {/* Product Grid & Top Bar */}
          <div className="flex-1 min-w-0">
            
            {/* Top Bar with count & sorting */}
            <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 mb-6 pb-4 border-b border-gray-200">
              <div>
                <h1 className="text-lg font-bold text-gray-900">
                  {activeCategory === 'All' ? 'All Products' : `${activeCategory} Collection`}
                </h1>
                <p className="text-xs text-gray-500 mt-0.5">
                  Showing {filteredProducts.length} {filteredProducts.length === 1 ? 'item' : 'items'}
                  {searchQuery ? ` matching "${searchQuery}"` : ''}
                </p>
              </div>

              <div className="flex items-center gap-2">
                <span className="text-xs text-gray-500 font-medium">Sort by:</span>
                <select
                  value={sortBy}
                  onChange={(e) => setSortBy(e.target.value)}
                  className="text-xs font-medium border border-gray-300 rounded-lg px-3 py-1.5 bg-white text-gray-700 focus:outline-none focus:ring-1 focus:ring-gray-900 cursor-pointer"
                >
                  <option value="newest">Newest First</option>
                  <option value="price-low">Price: Low to High</option>
                  <option value="price-high">Price: High to Low</option>
                  <option value="name">Product Name (A-Z)</option>
                </select>
              </div>
            </div>

            {/* Product Cards */}
            {paginatedList.length === 0 ? (
              <div className="bg-white border border-gray-200 rounded-xl p-12 text-center space-y-3">
                <p className="text-base font-bold text-gray-800">No products match your filters</p>
                <p className="text-xs text-gray-500 max-w-sm mx-auto">
                  Try adjusting the price range, switching categories, or clearing your active filters.
                </p>
                <button
                  onClick={clearAllFilters}
                  className="px-4 py-2 bg-gray-900 text-white rounded-lg text-xs font-semibold hover:bg-gray-800 transition cursor-pointer"
                >
                  Reset Filters
                </button>
              </div>
            ) : (
              <div className="grid grid-cols-2 md:grid-cols-3 gap-5">
                {paginatedList.map((p) => (
                  <div
                    key={p.id}
                    className="bg-white border border-gray-200 rounded-xl overflow-hidden hover:shadow-md transition-shadow group flex flex-col"
                  >
                    <div
                      onClick={() => navigateTo('details', { productId: p.id })}
                      className="h-52 bg-gray-100 overflow-hidden relative cursor-pointer"
                    >
                      <img
                        src={p.image || "https://images.unsplash.com/photo-1521572267360-ee0c2909d518?auto=format&fit=crop&w=600&q=80"}
                        alt={p.name}
                        className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
                      />
                      <span className="absolute top-2.5 left-2.5 px-2 py-0.5 text-[10px] font-bold uppercase bg-white/95 backdrop-blur-xs text-gray-800 rounded border border-gray-200 shadow-2xs">
                        {p.category}
                      </span>
                    </div>

                    <div className="p-4 flex-1 flex flex-col justify-between">
                      <div>
                        <h2
                          onClick={() => navigateTo('details', { productId: p.id })}
                          className="text-sm font-bold text-gray-900 hover:text-indigo-600 transition cursor-pointer truncate"
                          title={p.name}
                        >
                          {p.name}
                        </h2>
                        <p className="text-sm font-bold text-gray-700 mt-1">
                          ${Number(p.price).toFixed(2)}
                        </p>
                      </div>

                      <div className="pt-3 mt-2 border-t border-gray-100 flex gap-2">
                        <button
                          onClick={() => navigateTo('details', { productId: p.id })}
                          className="w-full py-1.5 px-3 text-xs font-semibold text-gray-700 bg-gray-50 hover:bg-gray-100 border border-gray-300 rounded-lg transition text-center cursor-pointer"
                        >
                          View Details
                        </button>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}

            {/* Pagination Controls */}
            {totalPages > 1 && (
              <div className="flex items-center justify-center gap-1.5 mt-10">
                <button
                  disabled={currentPage === 1}
                  onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
                  className="p-2 text-xs rounded-lg border border-gray-300 bg-white text-gray-600 hover:bg-gray-50 disabled:opacity-30 disabled:cursor-not-allowed cursor-pointer"
                >
                  <ChevronLeft size={14} />
                </button>
                {Array.from({ length: totalPages }).map((_, idx) => {
                  const pNum = idx + 1;
                  return (
                    <button
                      key={pNum}
                      onClick={() => setCurrentPage(pNum)}
                      className={`w-8 h-8 text-xs font-bold rounded-lg border transition-colors cursor-pointer ${
                        currentPage === pNum
                          ? 'bg-gray-900 text-white border-gray-900'
                          : 'bg-white text-gray-700 border-gray-300 hover:border-gray-500'
                      }`}
                    >
                      {pNum}
                    </button>
                  );
                })}
                <button
                  disabled={currentPage === totalPages}
                  onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))}
                  className="p-2 text-xs rounded-lg border border-gray-300 bg-white text-gray-600 hover:bg-gray-50 disabled:opacity-30 disabled:cursor-not-allowed cursor-pointer"
                >
                  <ChevronRight size={14} />
                </button>
              </div>
            )}

          </div>
        </div>

      </div>

      <Footer />
    </div>
  );
}
