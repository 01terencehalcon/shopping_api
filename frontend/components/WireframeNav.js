'use client';

import React, { useState } from 'react';
import { useApp } from '../lib/context';
import { Menu, X, Shield, User, CheckCircle2, AlertCircle } from 'lucide-react';

const SCREENS = [
  { id: 'login', label: 'Login' },
  { id: 'home', label: 'Home' },
  { id: 'listing', label: 'Product Listing' },
  { id: 'details', label: 'Product Details' },
  { id: 'cart', label: 'Shopping Cart' },
  { id: 'checkout', label: 'Checkout' },
  { id: 'profile', label: 'Profile' },
  { id: 'admin', label: 'Admin Dashboard' },
  { id: 'products', label: 'Product Mgmt' },
];

export default function WireframeNav() {
  const { screen, navigateTo, apiOnline, user, login, logout, showToast } = useApp();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  const handleQuickLogin = async (role) => {
    if (role === 'admin') {
      const res = await login('admin', 'Password123!');
      if (res.success) {
        showToast("Logged in as Admin (Full Access)");
      }
    } else {
      const res = await login('customer', 'Password123!');
      if (res.success) {
        showToast("Logged in as Customer");
      }
    }
  };

  return (
    <div className="bg-neutral-900 text-neutral-200 border-b border-neutral-800 text-xs select-none sticky top-0 z-50">
      <div className="max-w-7xl mx-auto px-4 py-2 flex items-center justify-between gap-4">
        
        {/* Left: Wireframe Label & Screen Switchers */}
        <div className="flex items-center gap-2">
          <button
            onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
            className="lg:hidden p-1.5 rounded hover:bg-neutral-800 text-neutral-300"
            aria-label="Toggle Navigation"
          >
            {mobileMenuOpen ? <X size={16} /> : <Menu size={16} />}
          </button>
          
          <div className="flex items-center gap-1.5 px-2 py-0.5 rounded bg-neutral-800 border border-neutral-700">
            <span className="w-1.5 h-1.5 rounded-full bg-indigo-400 animate-pulse"></span>
            <span className="font-bold tracking-wider uppercase text-[11px] text-neutral-300">Wireframe</span>
          </div>

          <div className="hidden lg:flex items-center gap-1">
            {SCREENS.map((s) => {
              const isActive = screen === s.id || (s.id === 'admin' && screen.startsWith('admin-'));
              return (
                <button
                  key={s.id}
                  onClick={() => navigateTo(s.id)}
                  className={`px-2.5 py-1 rounded transition-all font-medium ${
                    isActive
                      ? 'bg-neutral-100 text-neutral-950 font-semibold shadow-sm'
                      : 'text-neutral-300 hover:text-white hover:bg-neutral-800'
                  }`}
                >
                  {s.label}
                </button>
              );
            })}
          </div>
        </div>

        {/* Right: API Status & Quick Account Switcher */}
        <div className="flex items-center gap-3">
          {/* API Health Pill */}
          <div
            className={`hidden sm:flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-[11px] font-medium border ${
              apiOnline
                ? 'bg-emerald-950/60 border-emerald-700/60 text-emerald-300'
                : 'bg-amber-950/60 border-amber-700/60 text-amber-300'
            }`}
            title={apiOnline ? "Connected to ASP.NET Web API (http://localhost:43205)" : "Backend offline - Fallback seed data active"}
          >
            {apiOnline ? (
              <>
                <span className="w-1.5 h-1.5 rounded-full bg-emerald-400"></span>
                <span>API: Connected</span>
              </>
            ) : (
              <>
                <span className="w-1.5 h-1.5 rounded-full bg-amber-400"></span>
                <span>API: Offline Mode</span>
              </>
            )}
          </div>

          {/* Quick Demo Switcher */}
          <div className="flex items-center gap-1.5 bg-neutral-800/90 border border-neutral-700/80 rounded px-1.5 py-0.5">
            {user ? (
              <div className="flex items-center gap-2">
                <span className="text-neutral-400 text-[11px] hidden md:inline">
                  {user.roleId === 1 ? '👑' : '👤'} {user.username}
                </span>
                <button
                  onClick={logout}
                  className="text-[11px] text-neutral-300 hover:text-white underline cursor-pointer"
                >
                  Sign Out
                </button>
              </div>
            ) : (
              <div className="flex items-center gap-1 text-[11px]">
                <span className="text-neutral-400 hidden md:inline">Demo:</span>
                <button
                  onClick={() => handleQuickLogin('admin')}
                  className="px-1.5 py-0.5 rounded bg-neutral-700 hover:bg-neutral-600 text-neutral-200 transition"
                  title="Sign in as admin"
                >
                  Admin
                </button>
                <button
                  onClick={() => handleQuickLogin('customer')}
                  className="px-1.5 py-0.5 rounded bg-neutral-700 hover:bg-neutral-600 text-neutral-200 transition"
                  title="Sign in as customer"
                >
                  Customer
                </button>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Mobile Drawer */}
      {mobileMenuOpen && (
        <div className="lg:hidden border-t border-neutral-800 bg-neutral-900 px-4 py-3 grid grid-cols-2 gap-1.5 animate-in fade-in slide-in-from-top-2">
          {SCREENS.map((s) => (
            <button
              key={s.id}
              onClick={() => {
                navigateTo(s.id);
                setMobileMenuOpen(false);
              }}
              className={`text-left px-3 py-2 rounded text-xs transition ${
                screen === s.id ? 'bg-neutral-100 text-neutral-900 font-semibold' : 'text-neutral-300 hover:bg-neutral-800'
              }`}
            >
              {s.label}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
