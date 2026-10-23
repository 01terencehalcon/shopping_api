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
  { id: 'tracking', label: 'Order Tracking' },
  { id: 'admin', label: 'Admin Dashboard' },
  { id: 'products', label: 'Products' },
];

export default function WireframeNav() {
  const { screen, navigateTo, user, isLoggedIn, logout, canManageProducts } = useApp();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  const visibleScreens = SCREENS.filter((s) => {
    if (s.id === 'profile' || s.id === 'tracking') {
      return isLoggedIn;
    }
    if (s.id === 'products' || s.id === 'admin') {
      return canManageProducts;
    }
    return true;
  });

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
          
          <div className="hidden lg:flex items-center gap-1">
            {visibleScreens.map((s) => {
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


      </div>

      {/* Mobile Drawer */}
      {mobileMenuOpen && (
        <div className="lg:hidden border-t border-neutral-800 bg-neutral-900 px-4 py-3 grid grid-cols-2 gap-1.5 animate-in fade-in slide-in-from-top-2">
          {visibleScreens.map((s) => (
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
