'use client';

import React, { useState, useEffect, useRef, useMemo } from 'react';
import { MapContainer, TileLayer, Marker, Popup, useMap, useMapEvents } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { Navigation, Search, MapPin, AlertCircle, CheckCircle2 } from 'lucide-react';

// Custom modern SVG pin to avoid Leaflet asset path 404 bugs
const PickerIcon = L.divIcon({
  className: 'custom-picker-pin',
  iconSize: [40, 40],
  iconAnchor: [20, 40],
  popupAnchor: [0, -40],
  html: `
    <div style="position: relative; width: 40px; height: 40px; display: flex; align-items: center; justify-content: center; cursor: grab;">
      <div style="position: absolute; inset: -4px; border-radius: 9999px; background-color: rgba(225, 29, 72, 0.35); animation: ping 1.8s cubic-bezier(0, 0, 0.2, 1) infinite;"></div>
      <div style="width: 40px; height: 40px; border-radius: 9999px; background: linear-gradient(135deg, #e11d48, #be123c); display: flex; align-items: center; justify-content: center; box-shadow: 0 6px 16px -2px rgba(0,0,0,0.35); border: 2.5px solid #ffffff; color: #ffffff;">
        <span style="font-size: 18px; line-height: 1;">📍</span>
      </div>
    </div>
  `,
});

// Ensures Leaflet map renders correctly inside tabs/collapsibles
function InvalidateSizeOnMount() {
  const map = useMap();
  useEffect(() => {
    const timer = setTimeout(() => {
      try {
        map.invalidateSize();
      } catch (err) {
        console.warn('Map invalidateSize warning:', err);
      }
    }, 250);
    return () => clearTimeout(timer);
  }, [map]);
  return null;
}

// Click listener to place marker
function MapClickListener({ onPick }) {
  useMapEvents({
    click(e) {
      if (onPick) {
        onPick(e.latlng.lat, e.latlng.lng);
      }
    },
  });
  return null;
}

// Fly/Pan map to position updates
function MapController({ targetPos, zoom }) {
  const map = useMap();
  const lastTarget = useRef(null);

  useEffect(() => {
    if (!targetPos) return;
    const [lat, lng] = targetPos;
    if (lastTarget.current && lastTarget.current[0] === lat && lastTarget.current[1] === lng) {
      return;
    }
    lastTarget.current = targetPos;
    try {
      map.flyTo([lat, lng], zoom || 15, { duration: 1.2 });
    } catch {
      map.setView([lat, lng], zoom || 15);
    }
  }, [map, targetPos, zoom]);

  return null;
}

export default function LocationPickerMapInner({
  latitude,
  longitude,
  onChange,
  height = '360px',
  onAddressResolved = null,
}) {
  const [searchQuery, setSearchQuery] = useState('');
  const [isSearching, setIsSearching] = useState(false);
  const [geoLocating, setGeoLocating] = useState(false);
  const [statusMsg, setStatusMsg] = useState(null); // { type: 'success'|'error'|'info', text: string }
  const lastSearchTimeRef = useRef(0);

  // Default coordinates: Philippines (14.5995, 120.9842 at zoom 6) - Never New York!
  const defaultCenter = useMemo(() => {
    if (typeof latitude === 'number' && typeof longitude === 'number' && !isNaN(latitude) && !isNaN(longitude)) {
      return [latitude, longitude];
    }
    return [14.5995, 120.9842];
  }, [latitude, longitude]);

  const defaultZoom = typeof latitude === 'number' && typeof longitude === 'number' ? 15 : 6;

  const hasCoords = typeof latitude === 'number' && typeof longitude === 'number' && !isNaN(latitude) && !isNaN(longitude);

  const markerPosition = hasCoords ? [latitude, longitude] : null;

  // Handle manual map click
  const handleMapPick = (lat, lng) => {
    onChange(Number(lat.toFixed(6)), Number(lng.toFixed(6)));
    setStatusMsg({ type: 'success', text: `Pinned: ${lat.toFixed(4)}, ${lng.toFixed(4)}` });
  };

  // Draggable marker end handler
  const markerEventHandlers = useMemo(
    () => ({
      dragend(e) {
        const marker = e.target;
        if (marker) {
          const latLng = marker.getLatLng();
          onChange(Number(latLng.lat.toFixed(6)), Number(latLng.lng.toFixed(6)));
          setStatusMsg({ type: 'success', text: `Moved to: ${latLng.lat.toFixed(4)}, ${latLng.lng.toFixed(4)}` });
        }
      },
    }),
    [onChange]
  );

  // "Use my current location"
  const handleUseCurrentLocation = () => {
    if (typeof window === 'undefined' || !navigator.geolocation) {
      setStatusMsg({ type: 'error', text: 'Geolocation is not supported by your browser.' });
      return;
    }

    setGeoLocating(true);
    setStatusMsg({ type: 'info', text: 'Requesting GPS location...' });

    navigator.geolocation.getCurrentPosition(
      (position) => {
        const lat = Number(position.coords.latitude.toFixed(6));
        const lng = Number(position.coords.longitude.toFixed(6));
        onChange(lat, lng);
        setGeoLocating(false);
        setStatusMsg({ type: 'success', text: `GPS acquired: ${lat.toFixed(4)}, ${lng.toFixed(4)}` });
      },
      (err) => {
        setGeoLocating(false);
        let msg = 'Unable to retrieve location.';
        if (err.code === 1) msg = 'Location permission was denied. Please pick manually on the map.';
        else if (err.code === 2) msg = 'Location unavailable. Please pick manually.';
        else if (err.code === 3) msg = 'Location request timed out. Please try again.';
        setStatusMsg({ type: 'error', text: msg });
      },
      { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 }
    );
  };

  // Nominatim Address Search (strictly on button click or Enter key, max 1 req/sec)
  const handleSearch = async (e) => {
    if (e) e.preventDefault();
    const query = searchQuery.trim();
    if (!query) return;

    const now = Date.now();
    if (now - lastSearchTimeRef.current < 1000) {
      setStatusMsg({ type: 'info', text: 'Please wait a moment before searching again.' });
      return;
    }
    lastSearchTimeRef.current = now;

    setIsSearching(true);
    setStatusMsg({ type: 'info', text: `Searching "${query}"...` });

    try {
      const url = `https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}&limit=1&addressdetails=1`;
      const res = await fetch(url, {
        headers: {
          'Accept-Language': 'en',
        },
      });

      if (!res.ok) {
        throw new Error(`Nominatim error: ${res.status}`);
      }

      const results = await res.json();
      if (Array.isArray(results) && results.length > 0) {
        const match = results[0];
        const lat = Number(parseFloat(match.lat).toFixed(6));
        const lng = Number(parseFloat(match.lon).toFixed(6));
        onChange(lat, lng);

        if (onAddressResolved && match.address) {
          const addr = match.address;
          onAddressResolved({
            street: addr.road || addr.suburb || addr.neighbourhood || '',
            city: addr.city || addr.town || addr.municipality || addr.county || '',
            province: addr.state || addr.province || addr.region || '',
            postalCode: addr.postcode || '',
            country: addr.country || 'Philippines',
          });
        }

        setStatusMsg({ type: 'success', text: `Found: ${match.display_name.slice(0, 60)}...` });
      } else {
        setStatusMsg({ type: 'error', text: 'No location matches found. Try searching a city, barangay, or landmark.' });
      }
    } catch (err) {
      console.warn('Geocoding search failed:', err);
      setStatusMsg({ type: 'error', text: 'Geocoding search failed. You can click directly on the map to place your pin.' });
    } finally {
      setIsSearching(false);
    }
  };

  return (
    <div className="space-y-3">
      {/* Search Toolbar & Current Location Button */}
      <div className="flex flex-col sm:flex-row gap-2">
        <div className="flex-1 flex gap-2">
          <div className="relative flex-1">
            <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
            <input
              type="text"
              placeholder="Search city, town, or street (e.g. Davao City, Makati)..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === 'Enter') {
                  e.preventDefault();
                  handleSearch();
                }
              }}
              className="w-full pl-9 pr-3 py-2 text-xs bg-gray-50 border border-gray-300 rounded-lg focus:bg-white focus:outline-none focus:ring-1 focus:ring-gray-900"
            />
          </div>
          <button
            type="button"
            onClick={handleSearch}
            disabled={isSearching || !searchQuery.trim()}
            className="px-3.5 py-2 bg-gray-900 hover:bg-gray-800 disabled:opacity-50 text-white rounded-lg text-xs font-semibold flex items-center gap-1.5 transition cursor-pointer flex-shrink-0"
          >
            {isSearching ? (
              <span className="w-3.5 h-3.5 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
            ) : (
              <Search size={13} />
            )}
            <span>Search</span>
          </button>
        </div>

        <button
          type="button"
          onClick={handleUseCurrentLocation}
          disabled={geoLocating}
          className="px-3 py-2 bg-white hover:bg-gray-50 border border-gray-300 text-gray-700 rounded-lg text-xs font-semibold flex items-center justify-center gap-1.5 transition cursor-pointer flex-shrink-0 shadow-2xs"
          title="Use my current GPS location"
        >
          {geoLocating ? (
            <span className="w-3.5 h-3.5 border-2 border-blue-600 border-t-transparent rounded-full animate-spin"></span>
          ) : (
            <Navigation size={13} className="text-blue-600" />
          )}
          <span>Use My Current Location</span>
        </button>
      </div>

      {/* Map Container */}
      <div className="relative w-full rounded-xl overflow-hidden border border-gray-300 shadow-inner" style={{ height }}>
        <MapContainer
          center={defaultCenter}
          zoom={defaultZoom}
          scrollWheelZoom={true}
          style={{ width: '100%', height: '100%' }}
        >
          <TileLayer
            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
            maxZoom={19}
          />
          <InvalidateSizeOnMount />
          <MapClickListener onPick={handleMapPick} />
          {hasCoords && <MapController targetPos={[latitude, longitude]} zoom={15} />}

          {hasCoords && (
            <Marker
              position={markerPosition}
              icon={PickerIcon}
              draggable={true}
              eventHandlers={markerEventHandlers}
            >
              <Popup>
                <div className="text-xs p-1">
                  <p className="font-bold text-gray-900">Delivery Destination</p>
                  <p className="text-[11px] text-gray-600">
                    {latitude.toFixed(5)}, {longitude.toFixed(5)}
                  </p>
                  <p className="text-[10px] text-gray-400 mt-1">Drag marker to refine exact position</p>
                </div>
              </Popup>
            </Marker>
          )}
        </MapContainer>

        {/* Floating Instruction Overlay */}
        <div className="absolute top-2.5 left-2.5 z-[1000] bg-white/95 backdrop-blur-sm border border-gray-200 px-3 py-1.5 rounded-lg shadow-sm text-[11px] text-gray-700 flex items-center gap-1.5 pointer-events-none">
          <MapPin size={12} className="text-rose-600" />
          <span>Click anywhere or drag marker to set exact delivery pin</span>
        </div>
      </div>

      {/* Status / Coordinate Feedback */}
      <div className="flex flex-wrap items-center justify-between gap-2 text-xs">
        {statusMsg && (
          <div
            className={`flex items-center gap-1.5 text-[11px] font-medium ${
              statusMsg.type === 'error'
                ? 'text-red-600'
                : statusMsg.type === 'success'
                ? 'text-emerald-700'
                : 'text-blue-600'
            }`}
          >
            {statusMsg.type === 'error' ? (
              <AlertCircle size={13} />
            ) : statusMsg.type === 'success' ? (
              <CheckCircle2 size={13} />
            ) : (
              <Navigation size={13} />
            )}
            <span>{statusMsg.text}</span>
          </div>
        )}

        <div className="ml-auto text-[11px] text-gray-500 font-mono">
          {hasCoords ? (
            <span className="bg-emerald-50 text-emerald-800 border border-emerald-200 px-2.5 py-0.5 rounded-md font-bold">
              Coordinates: {latitude.toFixed(5)}, {longitude.toFixed(5)}
            </span>
          ) : (
            <span className="bg-amber-50 text-amber-800 border border-amber-200 px-2.5 py-0.5 rounded-md font-semibold">
              ⚠️ No location pinned yet
            </span>
          )}
        </div>
      </div>
    </div>
  );
}
