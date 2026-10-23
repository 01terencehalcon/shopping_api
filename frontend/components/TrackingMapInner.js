'use client';

import React, { useEffect, useMemo } from 'react';
import { MapContainer, TileLayer, Marker, Popup, Polyline, useMap, useMapEvents } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';

// Custom modern SVG DivIcons to completely eliminate Leaflet marker asset 404 bugs
function createMarkerIcon({ bgClass, iconHtml, pulse = false, size = 38 }) {
  return L.divIcon({
    className: 'custom-map-marker',
    iconSize: [size, size],
    iconAnchor: [size / 2, size],
    popupAnchor: [0, -size],
    html: `
      <div style="position: relative; width: ${size}px; height: ${size}px; display: flex; align-items: center; justify-content: center;">
        ${pulse ? `<div style="position: absolute; inset: -4px; border-radius: 9999px; background-color: rgba(59, 130, 246, 0.4); animation: ping 1.5s cubic-bezier(0, 0, 0.2, 1) infinite;"></div>` : ''}
        <div style="width: ${size}px; height: ${size}px; border-radius: 9999px; display: flex; align-items: center; justify-content: center; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.25); border: 2.5px solid #ffffff; ${bgClass}">
          ${iconHtml}
        </div>
      </div>
    `,
  });
}

const StoreIcon = createMarkerIcon({
  bgClass: 'background: linear-gradient(135deg, #059669, #10b981); color: #ffffff;',
  iconHtml: '<span style="font-size: 16px;">🏬</span>',
  size: 36,
});

const DestinationIcon = createMarkerIcon({
  bgClass: 'background: linear-gradient(135deg, #e11d48, #f43f5e); color: #ffffff;',
  iconHtml: '<span style="font-size: 16px;">📍</span>',
  size: 36,
});

const PackageIcon = createMarkerIcon({
  bgClass: 'background: linear-gradient(135deg, #2563eb, #3b82f6); color: #ffffff;',
  iconHtml: '<span style="font-size: 17px;">📦</span>',
  pulse: true,
  size: 40,
});

const CheckpointIcon = createMarkerIcon({
  bgClass: 'background: #374151; color: #f9fafb;',
  iconHtml: '<div style="width: 8px; height: 8px; background: #ffffff; border-radius: 9999px;"></div>',
  size: 24,
});

const SelectedPickerIcon = createMarkerIcon({
  bgClass: 'background: linear-gradient(135deg, #7c3aed, #9333ea); color: #ffffff;',
  iconHtml: '<span style="font-size: 16px;">🎯</span>',
  pulse: true,
  size: 38,
});

// Auto-adjust bounds to fit all markers
function MapBoundsManager({ points }) {
  const map = useMap();

  useEffect(() => {
    if (!points || points.length === 0) return;

    try {
      if (points.length === 1) {
        map.setView(points[0], 13);
      } else {
        const bounds = L.latLngBounds(points);
        map.fitBounds(bounds, {
          padding: [50, 50],
          maxZoom: 15,
          animate: true,
        });
      }
    } catch (err) {
      console.warn('Map fitBounds warning:', err);
    }
  }, [map, points]);

  return null;
}

// Click listener for interactive location picking
function MapClickPicker({ onPickLocation }) {
  useMapEvents({
    click(e) {
      if (onPickLocation) {
        onPickLocation(e.latlng.lat, e.latlng.lng);
      }
    },
  });
  return null;
}

export default function TrackingMapInner({
  storeLocation,
  deliveryAddress,
  checkpoints = [],
  interactivePick = false,
  pickedLocation = null,
  onPickLocation = null,
  height = '380px',
}) {
  // Collect coordinates for bounds and polyline
  const { polylineCoords, allValidPoints, latestCheckpoint } = useMemo(() => {
    const coords = [];
    const points = [];

    // Store origin
    if (storeLocation?.latitude && storeLocation?.longitude) {
      const p = [storeLocation.latitude, storeLocation.longitude];
      coords.push(p);
      points.push(p);
    }

    // Checkpoints in order
    if (Array.isArray(checkpoints)) {
      checkpoints.forEach((cp) => {
        if (typeof cp.latitude === 'number' && typeof cp.longitude === 'number') {
          const p = [cp.latitude, cp.longitude];
          coords.push(p);
          points.push(p);
        }
      });
    }

    // Delivery destination
    if (deliveryAddress?.latitude && deliveryAddress?.longitude) {
      const p = [deliveryAddress.latitude, deliveryAddress.longitude];
      points.push(p);
    }

    if (pickedLocation?.lat && pickedLocation?.lng) {
      points.push([pickedLocation.lat, pickedLocation.lng]);
    }

    const latest = checkpoints && checkpoints.length > 0 ? checkpoints[checkpoints.length - 1] : null;

    return {
      polylineCoords: coords,
      allValidPoints: points,
      latestCheckpoint: latest,
    };
  }, [storeLocation, deliveryAddress, checkpoints, pickedLocation]);

  const defaultCenter = allValidPoints.length > 0 ? allValidPoints[0] : [14.5995, 120.9842];

  return (
    <div className="relative w-full overflow-hidden rounded-2xl shadow-inner border border-gray-200" style={{ height }}>
      {interactivePick && (
        <div className="absolute top-3 left-3 z-[1000] bg-neutral-900/90 backdrop-blur-md text-white text-xs px-3.5 py-2 rounded-xl shadow-lg border border-neutral-700 flex items-center gap-2 pointer-events-none">
          <span className="w-2.5 h-2.5 rounded-full bg-emerald-400 animate-pulse"></span>
          <span>Click anywhere on the map to place checkpoint coordinates</span>
        </div>
      )}

      <MapContainer
        center={defaultCenter}
        zoom={12}
        scrollWheelZoom={true}
        style={{ height: '100%', width: '100%' }}
      >
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener noreferrer">OpenStreetMap</a> contributors'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />

        <MapBoundsManager points={allValidPoints} />
        {interactivePick && <MapClickPicker onPickLocation={onPickLocation} />}

        {/* Store / Pickup Marker */}
        {storeLocation?.latitude && storeLocation?.longitude && (
          <Marker position={[storeLocation.latitude, storeLocation.longitude]} icon={StoreIcon}>
            <Popup>
              <div className="p-1 space-y-1">
                <p className="font-bold text-xs text-gray-900">{storeLocation.label || 'Store / Fulfillment Hub'}</p>
                <p className="text-[11px] text-gray-500">Origin / Dispatch Point</p>
                <p className="text-[10px] font-mono text-gray-400">
                  {storeLocation.latitude.toFixed(4)}, {storeLocation.longitude.toFixed(4)}
                </p>
              </div>
            </Popup>
          </Marker>
        )}

        {/* Straight line polyline connecting checkpoints in order */}
        {polylineCoords.length >= 2 && (
          <Polyline
            positions={polylineCoords}
            pathOptions={{
              color: '#3b82f6',
              weight: 4,
              dashArray: '8, 8',
              opacity: 0.85,
              lineCap: 'round',
            }}
          />
        )}

        {/* Intermediate Checkpoints */}
        {checkpoints.map((cp, idx) => {
          if (!cp.latitude || !cp.longitude) return null;
          const isLatest = latestCheckpoint && latestCheckpoint.id === cp.id;

          // Don't duplicate if it's the exact latest checkpoint which gets the big PackageIcon
          if (isLatest) return null;

          return (
            <Marker key={cp.id || idx} position={[cp.latitude, cp.longitude]} icon={CheckpointIcon}>
              <Popup>
                <div className="p-1 space-y-1">
                  <div className="flex items-center gap-1.5">
                    <span className="px-1.5 py-0.5 rounded text-[10px] font-bold uppercase bg-gray-100 text-gray-800">
                      {cp.status}
                    </span>
                    <span className="text-[10px] text-gray-400">Step {idx + 1}</span>
                  </div>
                  {cp.note && <p className="text-xs text-gray-700">{cp.note}</p>}
                  <p className="text-[10px] text-gray-400">
                    {cp.createdAt ? new Date(cp.createdAt).toLocaleString() : ''}
                  </p>
                </div>
              </Popup>
            </Marker>
          );
        })}

        {/* Latest Package Position Marker */}
        {latestCheckpoint?.latitude && latestCheckpoint?.longitude && (
          <Marker position={[latestCheckpoint.latitude, latestCheckpoint.longitude]} icon={PackageIcon}>
            <Popup>
              <div className="p-1 space-y-1.5">
                <div className="flex items-center gap-1.5">
                  <span className="px-2 py-0.5 rounded-full text-[10px] font-black uppercase tracking-wider bg-blue-100 text-blue-800">
                    Latest Location: {latestCheckpoint.status}
                  </span>
                </div>
                {latestCheckpoint.note && (
                  <p className="text-xs font-medium text-gray-800">{latestCheckpoint.note}</p>
                )}
                <p className="text-[10px] text-gray-400">
                  Updated: {latestCheckpoint.createdAt ? new Date(latestCheckpoint.createdAt).toLocaleString() : 'Just now'}
                </p>
                <p className="text-[10px] font-mono text-gray-400">
                  {latestCheckpoint.latitude.toFixed(4)}, {latestCheckpoint.longitude.toFixed(4)}
                </p>
              </div>
            </Popup>
          </Marker>
        )}

        {/* Delivery Address Destination Marker */}
        {deliveryAddress?.latitude && deliveryAddress?.longitude && (
          <Marker position={[deliveryAddress.latitude, deliveryAddress.longitude]} icon={DestinationIcon}>
            <Popup>
              <div className="p-1 space-y-1">
                <p className="font-bold text-xs text-gray-900">Delivery Address</p>
                {deliveryAddress.recipientName && (
                  <p className="text-xs font-medium text-gray-800">{deliveryAddress.recipientName}</p>
                )}
                <p className="text-[11px] text-gray-600">
                  {[deliveryAddress.street, deliveryAddress.city, deliveryAddress.province, deliveryAddress.postalCode]
                    .filter(Boolean)
                    .join(', ')}
                </p>
                <p className="text-[10px] font-mono text-gray-400">
                  {deliveryAddress.latitude.toFixed(4)}, {deliveryAddress.longitude.toFixed(4)}
                </p>
              </div>
            </Popup>
          </Marker>
        )}

        {/* Admin Interactive Selected Pin */}
        {interactivePick && pickedLocation?.lat && pickedLocation?.lng && (
          <Marker position={[pickedLocation.lat, pickedLocation.lng]} icon={SelectedPickerIcon}>
            <Popup>
              <div className="p-1 text-xs">
                <p className="font-bold text-purple-700">Picked Location</p>
                <p className="font-mono text-[10px] text-gray-500">
                  Lat: {pickedLocation.lat.toFixed(5)}<br />
                  Lng: {pickedLocation.lng.toFixed(5)}
                </p>
              </div>
            </Popup>
          </Marker>
        )}
      </MapContainer>
    </div>
  );
}
