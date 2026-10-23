'use client';

import React, { useState, useEffect, useRef } from 'react';
import { useApp } from '../lib/context';
import { api } from '../lib/api';
import TrackingMap from '../components/TrackingMap';
import StoreHeader from '../components/StoreHeader';
import Footer from '../components/Footer';
import {
  ArrowLeft,
  Clock,
  Package,
  Truck,
  CheckCircle2,
  AlertCircle,
  RefreshCw,
  MapPin,
  Store,
  Navigation,
  FileText,
} from 'lucide-react';

const STATUS_ORDER = [
  { key: 'pending', label: 'Order Placed', icon: Clock },
  { key: 'packed', label: 'Packed', icon: Package },
  { key: 'shipped', label: 'Shipped', icon: Truck },
  { key: 'out_for_delivery', label: 'Out for Delivery', icon: Navigation },
  { key: 'delivered', label: 'Delivered', icon: CheckCircle2 },
];

export default function TrackingScreen({ orderId: propOrderId, onBack }) {
  const { navigateTo, selectedOrderId, isLoggedIn, isAuthLoading, showToast } = useApp();
  const orderId = propOrderId || selectedOrderId || 1042;

  useEffect(() => {
    if (!isAuthLoading && !isLoggedIn) {
      navigateTo('login');
    }
  }, [isLoggedIn, isAuthLoading, navigateTo]);

  const [trackingData, setTrackingData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [lastRefreshed, setLastRefreshed] = useState(null);
  const [isRefreshing, setIsRefreshing] = useState(false);

  const pollTimerRef = useRef(null);

  const fetchTracking = async (isManual = false) => {
    if (isManual) setIsRefreshing(true);
    try {
      const res = await api.orders.getTracking(orderId);
      if (res && res.success && res.data) {
        setTrackingData(res.data);
        setLastRefreshed(new Date());
        setError(null);
      } else if (res && res.data) {
        setTrackingData(res.data);
        setLastRefreshed(new Date());
        setError(null);
      } else if (res && res.error) {
        setError(res.error);
        setTrackingData(null);
      } else {
        setError('Tracking data is not available for this order.');
        setTrackingData(null);
      }
    } catch (err) {
      console.warn('Failed to fetch tracking data:', err);
      setError('Unable to load live tracking at this moment.');
    } finally {
      setLoading(false);
      setIsRefreshing(false);
    }
  };

  useEffect(() => {
    let mounted = true;
    fetchTracking();

    // 30 second polling
    pollTimerRef.current = setInterval(() => {
      if (mounted) {
        // If already delivered or cancelled, discontinue polling
        setTrackingData((current) => {
          const s = current?.status?.toLowerCase();
          if (s === 'delivered' || s === 'cancelled') {
            clearInterval(pollTimerRef.current);
            return current;
          }
          fetchTracking();
          return current;
        });
      }
    }, 30000);

    return () => {
      mounted = false;
      if (pollTimerRef.current) {
        clearInterval(pollTimerRef.current);
      }
    };
  }, [orderId]);

  const handleBack = () => {
    if (onBack) {
      onBack();
    } else {
      navigateTo('profile');
    }
  };

  const currentStatus = trackingData?.status?.toLowerCase() || 'pending';
  const isTerminalStatus = currentStatus === 'delivered' || currentStatus === 'cancelled';

  const getStatusBadgeClass = (status) => {
    switch (status?.toLowerCase()) {
      case 'delivered':
        return 'bg-emerald-100 text-emerald-800 border-emerald-200';
      case 'shipped':
      case 'out_for_delivery':
        return 'bg-blue-100 text-blue-800 border-blue-200';
      case 'cancelled':
        return 'bg-rose-100 text-rose-800 border-rose-200';
      case 'packed':
        return 'bg-purple-100 text-purple-800 border-purple-200';
      default:
        return 'bg-amber-100 text-amber-800 border-amber-200';
    }
  };

  // Find index in standard timeline
  const activeStepIdx = STATUS_ORDER.findIndex(
    (step) => step.key === currentStatus || (currentStatus === 'out_for_delivery' && step.key === 'shipped')
  );

  if (isAuthLoading || !isLoggedIn) {
    return null;
  }

  return (
    <div className="min-h-screen bg-gray-50 flex flex-col font-sans">
      <StoreHeader />

      <main className="flex-1 max-w-4xl mx-auto px-3 sm:px-6 py-6 w-full">
        {/* Top Header & Actions */}
        <div className="flex flex-wrap items-center justify-between gap-3 mb-5">
          <div className="flex items-center gap-3">
            <button
              onClick={handleBack}
              className="p-2 rounded-xl bg-white border border-gray-200 hover:bg-gray-100 text-gray-700 transition shadow-2xs cursor-pointer"
              title="Return to order history"
            >
              <ArrowLeft size={16} />
            </button>
            <div>
              <div className="flex items-center gap-2">
                <h1 className="text-base sm:text-lg font-black text-gray-900 tracking-tight">
                  Track Order #ORD-{orderId}
                </h1>
                <span
                  className={`px-2.5 py-0.5 rounded-full text-[11px] font-bold border uppercase tracking-wider ${getStatusBadgeClass(
                    currentStatus
                  )}`}
                >
                  {currentStatus.replace(/_/g, ' ')}
                </span>
              </div>
              <p className="text-xs text-gray-500 mt-0.5">
                Real-time transit checkpoint telemetry & delivery route
              </p>
            </div>
          </div>

          <div className="flex items-center gap-2">
            {!isTerminalStatus && (
              <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-emerald-50 border border-emerald-200 text-emerald-700 text-[11px] font-medium">
                <span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse"></span>
                <span>Live polling (30s)</span>
              </div>
            )}
            <button
              onClick={() => fetchTracking(true)}
              disabled={isRefreshing}
              className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-white border border-gray-200 text-gray-700 hover:bg-gray-50 text-xs font-semibold shadow-2xs transition cursor-pointer"
            >
              <RefreshCw size={13} className={isRefreshing ? 'animate-spin text-blue-600' : ''} />
              <span>{isRefreshing ? 'Updating...' : 'Refresh'}</span>
            </button>
          </div>
        </div>

        {error && (
          <div className="mb-4 p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-xs flex items-center gap-2">
            <AlertCircle size={15} />
            <span>{error}</span>
          </div>
        )}

        {/* Map Section (Top) */}
        <section className="bg-white rounded-2xl border border-gray-200 p-2 sm:p-3 shadow-xs mb-6">
          <div className="px-3 py-2 flex items-center justify-between text-xs text-gray-500">
            <div className="flex items-center gap-1.5 font-semibold text-gray-700">
              <Navigation size={14} className="text-blue-600" />
              <span>Interactive Delivery Map</span>
            </div>
            {lastRefreshed && (
              <span className="text-[10px] text-gray-400">
                Last checked: {lastRefreshed.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' })}
              </span>
            )}
          </div>

          {loading ? (
            <div className="w-full h-[360px] rounded-xl bg-gray-100 flex flex-col items-center justify-center gap-2 text-gray-400 animate-pulse">
              <RefreshCw size={24} className="animate-spin text-blue-500" />
              <span className="text-xs">Loading street map & telemetry...</span>
            </div>
          ) : (
            <TrackingMap
              storeLocation={trackingData?.storeLocation}
              deliveryAddress={trackingData?.deliveryAddress}
              checkpoints={trackingData?.checkpoints || []}
              height="380px"
            />
          )}

          {/* Quick Map Legend */}
          <div className="mt-3 pt-3 border-t border-gray-100 px-3 flex flex-wrap items-center justify-between gap-3 text-xs text-gray-600">
            <div className="flex flex-wrap items-center gap-4 text-[11px]">
              <div className="flex items-center gap-1.5">
                <span className="w-3 h-3 rounded-full bg-emerald-500 border-2 border-white shadow-xs"></span>
                <span className="font-medium text-gray-700">Store / Pickup</span>
              </div>
              <div className="flex items-center gap-1.5">
                <span className="w-3 h-3 rounded-full bg-blue-600 border-2 border-white shadow-xs"></span>
                <span className="font-medium text-gray-700">Latest Package Position</span>
              </div>
              <div className="flex items-center gap-1.5">
                <span className="w-3 h-3 rounded-full bg-rose-500 border-2 border-white shadow-xs"></span>
                <span className="font-medium text-gray-700">Delivery Destination</span>
              </div>
              <div className="flex items-center gap-1.5">
                <span className="w-5 h-0.5 border-t-2 border-dashed border-blue-500"></span>
                <span className="font-medium text-gray-700">Transit Path</span>
              </div>
            </div>
          </div>
        </section>

        {/* Milestone Steps Bar */}
        <section className="bg-white rounded-2xl border border-gray-200 p-4 sm:p-6 shadow-xs mb-6">
          <h3 className="text-xs font-bold text-gray-400 uppercase tracking-wider mb-4">
            Delivery Milestones
          </h3>
          <div className="grid grid-cols-5 gap-1 sm:gap-2">
            {STATUS_ORDER.map((step, idx) => {
              const Icon = step.icon;
              const isCompleted = activeStepIdx >= idx && currentStatus !== 'cancelled';
              const isCurrent = activeStepIdx === idx && currentStatus !== 'cancelled';

              return (
                <div key={step.key} className="flex flex-col items-center text-center">
                  <div
                    className={`w-8 h-8 sm:w-10 sm:h-10 rounded-full flex items-center justify-center transition mb-1.5 ${
                      isCompleted
                        ? 'bg-blue-600 text-white shadow-sm'
                        : 'bg-gray-100 text-gray-400 border border-gray-200'
                    } ${isCurrent ? 'ring-4 ring-blue-100' : ''}`}
                  >
                    <Icon size={16} />
                  </div>
                  <span
                    className={`text-[10px] sm:text-xs font-semibold leading-tight ${
                      isCompleted ? 'text-gray-900' : 'text-gray-400'
                    }`}
                  >
                    {step.label}
                  </span>
                </div>
              );
            })}
          </div>
        </section>

        {/* Two Columns: Destination Info & Checkpoints Timeline */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {/* Left Column: Route Summary Cards */}
          <div className="space-y-4 md:col-span-1">
            <div className="bg-white rounded-2xl border border-gray-200 p-4 shadow-xs space-y-3">
              <div className="flex items-center gap-2 text-xs font-bold text-gray-900 uppercase tracking-wider">
                <Store size={15} className="text-emerald-600" />
                <span>Origin Hub</span>
              </div>
              <div className="text-xs text-gray-600">
                <p className="font-bold text-gray-800">
                  {trackingData?.storeLocation?.label || 'StyleShop Central Hub'}
                </p>
                {trackingData?.storeLocation?.latitude && (
                  <p className="text-[11px] font-mono text-gray-400 mt-0.5">
                    {trackingData.storeLocation.latitude.toFixed(4)}, {trackingData.storeLocation.longitude.toFixed(4)}
                  </p>
                )}
              </div>
            </div>

            <div className="bg-white rounded-2xl border border-gray-200 p-4 shadow-xs space-y-3">
              <div className="flex items-center gap-2 text-xs font-bold text-gray-900 uppercase tracking-wider">
                <MapPin size={15} className="text-rose-600" />
                <span>Shipping Address</span>
              </div>
              {trackingData?.deliveryAddress ? (
                <div className="text-xs text-gray-600 space-y-1">
                  {trackingData.deliveryAddress.recipientName && (
                    <p className="font-bold text-gray-900">{trackingData.deliveryAddress.recipientName}</p>
                  )}
                  <p>{trackingData.deliveryAddress.street}</p>
                  <p>
                    {[trackingData.deliveryAddress.city, trackingData.deliveryAddress.province, trackingData.deliveryAddress.postalCode]
                      .filter(Boolean)
                      .join(', ')}
                  </p>
                  {trackingData.deliveryAddress.latitude && (
                    <p className="text-[11px] font-mono text-gray-400 mt-1">
                      Coordinates: {trackingData.deliveryAddress.latitude.toFixed(4)},{' '}
                      {trackingData.deliveryAddress.longitude.toFixed(4)}
                    </p>
                  )}
                </div>
              ) : (
                <p className="text-xs text-gray-400">Standard Customer Shipping Address</p>
              )}
            </div>
          </div>

          {/* Right Column: Checkpoints Status Timeline (Below Map) */}
          <div className="md:col-span-2 bg-white rounded-2xl border border-gray-200 p-5 shadow-xs">
            <div className="flex items-center justify-between pb-3 border-b border-gray-100 mb-4">
              <div>
                <h3 className="text-sm font-bold text-gray-900">Checkpoint Timeline</h3>
                <p className="text-xs text-gray-400 mt-0.5">Sequential location updates recorded by fulfillment carriers</p>
              </div>
              <span className="text-xs font-semibold text-gray-500">
                {trackingData?.checkpoints?.length || 0} checkpoints
              </span>
            </div>

            {(!trackingData?.checkpoints || trackingData.checkpoints.length === 0) ? (
              <div className="py-12 text-center text-xs text-gray-400">
                No checkpoints recorded yet for this order.
              </div>
            ) : (
              <div className="relative pl-6 space-y-6 before:absolute before:left-2.5 before:top-2 before:bottom-2 before:w-0.5 before:bg-gray-200">
                {trackingData.checkpoints
                  .slice()
                  .reverse() // show latest on top
                  .map((cp, idx) => {
                    const isLatest = idx === 0;

                    return (
                      <div key={cp.id || idx} className="relative group">
                        {/* Dot indicator */}
                        <div
                          className={`absolute -left-6 top-1 w-3.5 h-3.5 rounded-full border-2 border-white transition shadow-2xs ${
                            isLatest
                              ? 'bg-blue-600 ring-4 ring-blue-100'
                              : 'bg-gray-400 group-hover:bg-gray-600'
                          }`}
                        />

                        <div className="space-y-1">
                          <div className="flex flex-wrap items-center gap-2">
                            <span
                              className={`px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider ${
                                isLatest
                                  ? 'bg-blue-600 text-white'
                                  : 'bg-gray-100 text-gray-700'
                              }`}
                            >
                              {cp.status.replace(/_/g, ' ')}
                            </span>
                            <span className="text-[11px] text-gray-400 font-medium">
                              {cp.createdAt ? new Date(cp.createdAt).toLocaleString() : 'Recent update'}
                            </span>
                          </div>

                          {cp.note && (
                            <p className="text-xs text-gray-800 font-medium pt-0.5 leading-relaxed">
                              {cp.note}
                            </p>
                          )}

                          <div className="flex items-center gap-2 text-[10px] text-gray-400 font-mono">
                            <span>Lat: {cp.latitude?.toFixed(4)}</span>
                            <span>•</span>
                            <span>Lng: {cp.longitude?.toFixed(4)}</span>
                          </div>
                        </div>
                      </div>
                    );
                  })}
              </div>
            )}
          </div>
        </div>
      </main>

      <Footer />
    </div>
  );
}
