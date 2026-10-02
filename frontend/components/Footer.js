'use client';

import React from 'react';
import { useApp } from '../lib/context';

export default function Footer() {
  const { navigateTo } = useApp();

  const columns = [
    {
      heading: "StyleShop",
      links: [
        { label: "About Us", action: () => navigateTo('home') },
        { label: "Careers", action: () => {} },
        { label: "Sustainability", action: () => {} },
        { label: "Press", action: () => {} }
      ]
    },
    {
      heading: "Shop Categories",
      links: [
        { label: "Tops & Shirts", action: () => navigateTo('listing', { category: 'Tops' }) },
        { label: "Bottoms & Pants", action: () => navigateTo('listing', { category: 'Bottoms' }) },
        { label: "Shoes & Footwear", action: () => navigateTo('listing', { category: 'Shoes' }) },
        { label: "Accessories", action: () => navigateTo('listing', { category: 'Accessories' }) }
      ]
    },
    {
      heading: "Customer Support",
      links: [
        { label: "Order Tracking", action: () => navigateTo('profile') },
        { label: "Shipping Policy", action: () => {} },
        { label: "Returns & Exchanges", action: () => {} },
        { label: "FAQ & Help Center", action: () => {} }
      ]
    },
    {
      heading: "Follow Our Styles",
      links: [
        { label: "Instagram @StyleShop", action: () => {} },
        { label: "Pinterest Trends", action: () => {} },
        { label: "TikTok Lookbook", action: () => {} },
        { label: "YouTube Styling", action: () => {} }
      ]
    }
  ];

  return (
    <footer className="bg-gray-100 border-t border-gray-200 mt-16 text-gray-600">
      <div className="max-w-6xl mx-auto px-6 py-12">
        <div className="grid grid-cols-2 md:grid-cols-4 gap-8">
          {columns.map((col) => (
            <div key={col.heading}>
              <p className="text-xs font-bold text-gray-900 uppercase tracking-wider mb-3.5">
                {col.heading}
              </p>
              <ul className="space-y-2">
                {col.links.map((link) => (
                  <li key={link.label}>
                    <button
                      onClick={link.action}
                      className="text-xs text-gray-500 hover:text-gray-900 transition-colors text-left"
                    >
                      {link.label}
                    </button>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>

        <div className="mt-10 pt-6 border-t border-gray-200 flex flex-col sm:flex-row justify-between items-center gap-4 text-xs text-gray-400">
          <p>© 2025 STYLESHOP Systems Inc. All rights reserved. Connected to ASP.NET Core API.</p>
          <div className="flex items-center gap-2">
            {["Visa", "Mastercard", "American Express", "PayPal", "Apple Pay"].map((badge) => (
              <span
                key={badge}
                className="px-2 py-0.5 rounded text-[10px] font-medium bg-white border border-gray-200 text-gray-600 shadow-2xs"
              >
                {badge}
              </span>
            ))}
          </div>
        </div>
      </div>
    </footer>
  );
}
