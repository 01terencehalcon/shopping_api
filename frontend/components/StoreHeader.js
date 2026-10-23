'use client';

import React, { useState } from 'react';
import { useApp } from '../lib/context';
import { Search, ShoppingCart, User, Shield, LogOut, ChevronDown, Check, Package } from 'lucide-react';

export default function StoreHeader() {
  const {
    screen,
    navigateTo,
    totalItemCount,
    user,
    isAdmin,
    isStaff,
    canManageProducts,
    logout,
    searchQuery,
    setSearchQuery,
    setSelectedCategory
  } = useApp();

  const [searchInput, setSearchInput] = useState(searchQuery || '');
  const [profileDropdownOpen, setProfileDropdownOpen] = useState(false);

  const handleSearchSubmit = (e) => {
    e.preventDefault();
    if (searchInput.trim()) {
      navigateTo('listing', { search: searchInput.trim(), category: 'All' });
    } else {
      navigateTo('listing', { search: '', category: 'All' });
    }
  };

  const handleCategoryNav = (cat) => {
    setSelectedCategory(cat);
    if (cat === 'Home') {
      navigateTo('home');
    } else {
      navigateTo('listing', { category: cat, search: '' });
    }
  };

  return (
    <header className="bg-white border-b border-gray-200 sticky top-9 z-40 shadow-xs">
      <div className="max-w-6xl mx-auto px-4 sm:px-6 py-3.5 flex items-center justify-between gap-4">
        
        {/* Brand Logo */}
        <div className="flex items-center gap-6">
          <button
            onClick={() => navigateTo('home')}
            className="text-lg font-black tracking-tight text-gray-900 flex items-center gap-2 hover:opacity-85 transition cursor-pointer"
          >
            <span className="w-6 h-6 bg-gray-900 text-white rounded flex items-center justify-center text-xs font-bold">
              S
            </span>
            <span>STYLESHOP</span>
          </button>

          {/* Navigation Links */}
          <nav className="hidden md:flex items-center gap-1">
            {['Home', 'Tops', 'Bottoms', 'Shoes', 'Accessories'].map((item) => (
              <button
                key={item}
                onClick={() => handleCategoryNav(item)}
                className={`px-3 py-1.5 text-sm font-medium rounded-md transition-colors ${
                  (item === 'Home' && screen === 'home') || (screen === 'listing')
                    ? 'text-gray-900 hover:bg-gray-100'
                    : 'text-gray-600 hover:text-gray-900 hover:bg-gray-50'
                }`}
              >
                {item}
              </button>
            ))}
          </nav>
        </div>

        {/* Search Bar */}
        <div className="flex-1 max-w-sm relative hidden sm:block">
          <form onSubmit={handleSearchSubmit}>
            <Search size={15} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
            <input
              type="text"
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              placeholder="Search clothes, shoes, accessories..."
              className="w-full pl-9 pr-4 py-2 text-sm bg-gray-100 border border-transparent rounded-lg placeholder-gray-400 focus:bg-white focus:border-gray-400 focus:ring-1 focus:ring-gray-400 focus:outline-none transition"
            />
          </form>
        </div>

        {/* Right Actions: Cart & Profile */}
        <div className="flex items-center gap-2 relative">
          
          {/* Admin shortcut button for Staff and Admin */}
          {(isAdmin || isStaff) && (
            <button
              onClick={() => navigateTo('admin')}
              className="hidden lg:flex items-center gap-1.5 px-2.5 py-1.5 text-xs font-medium bg-gray-100 hover:bg-gray-200 text-gray-800 rounded-md border border-gray-200 transition cursor-pointer"
              title="Open Admin Dashboard"
            >
              <Shield size={13} className="text-gray-700" />
              <span>Admin Panel</span>
            </button>
          )}

          {/* Products shortcut button for Staff and Admin */}
          {canManageProducts && (
            <button
              onClick={() => navigateTo('products')}
              className="hidden lg:flex items-center gap-1.5 px-2.5 py-1.5 text-xs font-medium bg-gray-900 hover:bg-gray-800 text-white rounded-md transition cursor-pointer"
              title="Open Products"
            >
              <Package size={13} />
              <span>Products</span>
            </button>
          )}

          {/* Cart Button with badge */}
          <button
            onClick={() => navigateTo('cart')}
            className="relative p-2 text-gray-700 hover:bg-gray-100 rounded-lg transition cursor-pointer flex items-center justify-center"
            aria-label="Shopping Cart"
          >
            <ShoppingCart size={20} />
            {totalItemCount > 0 && (
              <span className="absolute -top-1 -right-1 min-w-[18px] h-[18px] bg-gray-900 text-white text-[11px] font-bold rounded-full flex items-center justify-center px-1 animate-in zoom-in-50">
                {totalItemCount}
              </span>
            )}
          </button>

          {/* User Account / Profile */}
          <div className="relative">
            {user ? (
              <div>
                <button
                  onClick={() => setProfileDropdownOpen(!profileDropdownOpen)}
                  className="flex items-center gap-2 p-1.5 pl-2.5 pr-2 rounded-lg border border-gray-200 hover:border-gray-300 hover:bg-gray-50 transition cursor-pointer"
                >
                  <div className="w-6 h-6 rounded-full bg-gray-800 text-white flex items-center justify-center text-xs font-bold uppercase">
                    {user.username ? user.username.charAt(0) : 'U'}
                  </div>
                  <span className="text-xs font-medium text-gray-800 max-w-[80px] truncate hidden md:inline">
                    {user.username}
                  </span>
                  <ChevronDown size={14} className="text-gray-400" />
                </button>

                {profileDropdownOpen && (
                  <div
                    className="absolute right-0 mt-2 w-52 bg-white rounded-xl shadow-lg border border-gray-200 py-1.5 z-50 text-sm animate-in fade-in slide-in-from-top-2"
                    onMouseLeave={() => setProfileDropdownOpen(false)}
                  >
                    <div className="px-3.5 py-2 border-b border-gray-100">
                      <p className="font-semibold text-gray-900 truncate">{user.username}</p>
                      <p className="text-xs text-gray-500 truncate">{user.email || 'customer@styleshop.com'}</p>
                      <span className="inline-block mt-1 px-1.5 py-0.5 rounded text-[10px] font-medium bg-gray-100 text-gray-700">
                        {user.roleId === 1 ? '👑 Administrator' : user.roleId === 2 ? '🛡️ Staff Member' : '👤 Customer'}
                      </span>
                    </div>

                    <button
                      onClick={() => {
                        navigateTo('profile');
                        setProfileDropdownOpen(false);
                      }}
                      className="w-full text-left px-3.5 py-2 text-gray-700 hover:bg-gray-50 flex items-center gap-2 cursor-pointer"
                    >
                      <User size={15} />
                      <span>My Profile & Orders</span>
                    </button>

                    {canManageProducts && (
                      <button
                        onClick={() => {
                          navigateTo('products');
                          setProfileDropdownOpen(false);
                        }}
                        className="w-full text-left px-3.5 py-2 text-gray-700 hover:bg-gray-50 flex items-center gap-2 cursor-pointer"
                      >
                        <Package size={15} />
                        <span>Products</span>
                      </button>
                    )}

                    {(isAdmin || isStaff) && (
                      <button
                        onClick={() => {
                          navigateTo('admin');
                          setProfileDropdownOpen(false);
                        }}
                        className="w-full text-left px-3.5 py-2 text-gray-700 hover:bg-gray-50 flex items-center gap-2 cursor-pointer"
                      >
                        <Shield size={15} />
                        <span>Admin Dashboard</span>
                      </button>
                    )}

                    <div className="border-t border-gray-100 my-1"></div>

                    <button
                      onClick={() => {
                        logout();
                        setProfileDropdownOpen(false);
                      }}
                      className="w-full text-left px-3.5 py-2 text-red-600 hover:bg-red-50 flex items-center gap-2"
                    >
                      <LogOut size={15} />
                      <span>Sign Out</span>
                    </button>
                  </div>
                )}
              </div>
            ) : (
              <button
                onClick={() => navigateTo('login')}
                className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold bg-gray-900 text-white rounded-lg hover:bg-gray-800 transition cursor-pointer"
              >
                <User size={14} />
                <span>Sign In</span>
              </button>
            )}
          </div>
        </div>
      </div>
    </header>
  );
}
