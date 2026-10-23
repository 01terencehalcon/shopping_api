'use client';

import React from 'react';
import dynamic from 'next/dynamic';
import { Navigation } from 'lucide-react';

const DynamicLocationPickerInner = dynamic(() => import('./LocationPickerMapInner'), {
  ssr: false,
  loading: () => (
    <div className="w-full h-[360px] rounded-xl bg-gray-100 flex flex-col items-center justify-center gap-3 border border-gray-200 animate-pulse text-gray-500">
      <Navigation size={26} className="animate-spin text-gray-600" />
      <span className="text-xs font-semibold">Loading interactive map...</span>
    </div>
  ),
});

export default function LocationPickerMap(props) {
  return <DynamicLocationPickerInner {...props} />;
}
