'use client';

import React, { Suspense } from 'react';
import { AppProvider, useApp } from '../lib/context';
import WireframeNav from '../components/WireframeNav';
import HomeScreen from '../screens/HomeScreen';
import LoginScreen from '../screens/LoginScreen';
import ListingScreen from '../screens/ListingScreen';
import DetailsScreen from '../screens/DetailsScreen';
import CartScreen from '../screens/CartScreen';
import CheckoutScreen from '../screens/CheckoutScreen';
import ProfileScreen from '../screens/ProfileScreen';
import AdminDashboard from '../screens/AdminDashboard';
import ProductManagement from '../screens/ProductManagement';
import { CheckCircle2, AlertCircle, Info } from 'lucide-react';

function ScreenRenderer() {
  const { screen, toast } = useApp();

  const renderCurrentScreen = () => {
    switch (screen) {
      case 'login':
        return <LoginScreen />;
      case 'home':
        return <HomeScreen />;
      case 'listing':
        return <ListingScreen />;
      case 'details':
        return <DetailsScreen />;
      case 'cart':
        return <CartScreen />;
      case 'checkout':
        return <CheckoutScreen />;
      case 'profile':
        return <ProfileScreen />;
      case 'admin':
      case 'admin-orders':
      case 'admin-customers':
      case 'admin-reports':
        return <AdminDashboard />;
      case 'products':
        return <ProductManagement />;
      default:
        return <HomeScreen />;
    }
  };

  return (
    <div className="flex-1 flex flex-col min-h-screen relative">
      {/* Wireframe Bar */}
      <WireframeNav />

      {/* Active Screen */}
      <div className="flex-1">
        {renderCurrentScreen()}
      </div>

      {/* Global Toast Notification */}
      {toast && (
        <div className="fixed bottom-5 right-5 z-50 animate-in slide-in-from-bottom-3 duration-300">
          <div
            className={`flex items-center gap-2.5 px-4 py-3 rounded-xl shadow-lg border text-xs font-semibold ${
              toast.type === 'error'
                ? 'bg-red-950/90 text-red-200 border-red-800'
                : toast.type === 'info'
                ? 'bg-neutral-900/90 text-neutral-200 border-neutral-700'
                : 'bg-emerald-950/90 text-emerald-200 border-emerald-800'
            }`}
          >
            {toast.type === 'error' ? (
              <AlertCircle size={16} className="text-red-400" />
            ) : toast.type === 'info' ? (
              <Info size={16} className="text-blue-400" />
            ) : (
              <CheckCircle2 size={16} className="text-emerald-400" />
            )}
            <span>{toast.message}</span>
          </div>
        </div>
      )}
    </div>
  );
}

export default function Page() {
  return (
    <Suspense fallback={<div className="min-h-screen flex items-center justify-center text-xs text-gray-400">Loading StyleShop...</div>}>
      <AppProvider>
        <ScreenRenderer />
      </AppProvider>
    </Suspense>
  );
}
