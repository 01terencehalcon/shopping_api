'use client';

import React from 'react';
import dynamic from 'next/dynamic';
import { MapPin, Navigation } from 'lucide-react';

const DynamicMapInner = dynamic(() => import('./TrackingMapInner'), {
  ssr: false,
  loading: () => (
    <div className="w-full h-[380px] rounded-2xl bg-gray-100 flex flex-col items-center justify-center gap-3 border border-gray-200 animate-pulse text-gray-500">
      <Navigation size={28} className="animate-spin text-blue-500" />
      <span className="text-xs font-semibold">Loading street map tiles...</span>
    </div>
  ),
});

export default function TrackingMap({
  storeLocation,
  deliveryAddress,
  checkpoints = [],
  interactivePick = false,
  pickedLocation = null,
  onPickLocation = null,
  height = '380px',
}) {
  const hasStoreCoords = Boolean(storeLocation?.latitude && storeLocation?.longitude);
  const hasDeliveryCoords = Boolean(deliveryAddress?.latitude && deliveryAddress?.longitude);
  const hasCheckpointCoords = Array.isArray(checkpoints) && checkpoints.some(c => typeof c.latitude === 'number' && typeof c.longitude === 'number');
  const hasPickedCoords = Boolean(pickedLocation?.lat && pickedLocation?.lng);

  // If order has no coordinates at all (and not in interactive picking mode)
  if (!interactivePick && !hasStoreCoords && !hasDeliveryCoords && !hasCheckpointCoords && !hasPickedCoords) {
    return (
      <div
        className="w-full rounded-2xl bg-gradient-to-br from-gray-50 to-gray-100 border border-dashed border-gray-300 flex flex-col items-center justify-center p-8 text-center space-y-3"
        style={{ height }}
      >
        <div className="w-12 h-12 rounded-2xl bg-amber-50 text-amber-600 flex items-center justify-center border border-amber-200 shadow-xs">
          <MapPin size={24} />
        </div>
        <div>
          <h4 className="text-sm font-bold text-gray-800">No Tracking Coordinates Yet</h4>
          <p className="text-xs text-gray-500 max-w-sm mt-1">
            Tracking checkpoints will appear on the street map once our dispatch team scans and routes your package.
          </p>
        </div>
      </div>
    );
  }

  return (
    <DynamicMapInner
      storeLocation={storeLocation}
      deliveryAddress={deliveryAddress}
      checkpoints={checkpoints}
      interactivePick={interactivePick}
      pickedLocation={pickedLocation}
      onPickLocation={onPickLocation}
      height={height}
    />
  );
}
