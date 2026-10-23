'use client';

import React, { useState, useEffect } from 'react';
import { useApp } from '../lib/context';
import { api } from '../lib/api';
import {
  BarChart2,
  Package,
  ShoppingBag,
  Users,
  FileText,
  LogOut,
  ArrowUpRight,
  TrendingUp,
  Store,
  CheckCircle,
  Clock,
  Truck,
  Shield,
  MapPin,
  X,
  Navigation,
  RefreshCw,
  UserCheck,
  Check
} from 'lucide-react';
import TrackingMap from '../components/TrackingMap';

export default function AdminDashboard() {
  const { navigateTo, user, logout, showToast, canManageProducts, isAuthLoading } = useApp();

  const [orders, setOrders] = useState([]);
  const [productsCount, setProductsCount] = useState(10);
  const [usersCount, setUsersCount] = useState(3);
  const [loading, setLoading] = useState(true);

  // Section Routing & Staff Requests State
  const [currentSection, setCurrentSection] = useState('overview');
  const [pendingStaff, setPendingStaff] = useState([]);
  const [loadingPending, setLoadingPending] = useState(false);
  const [actioningId, setActioningId] = useState(null);

  // Tracking Modal State
  const [trackingModalOrder, setTrackingModalOrder] = useState(null);
  const [trackingStatus, setTrackingStatus] = useState('shipped');
  const [trackingNote, setTrackingNote] = useState('');
  const [pickedCoords, setPickedCoords] = useState({ lat: 40.7306, lng: -73.9352 });
  const [submittingTracking, setSubmittingTracking] = useState(false);
  const [existingTracking, setExistingTracking] = useState(null);
  const [loadingTracking, setLoadingTracking] = useState(false);

  // Redirect non-staff/admin users to /login
  useEffect(() => {
    if (!isAuthLoading) {
      const isStaffOrAdmin = user && (user.roleId === 1 || user.roleId === 2 || user.role === 'Admin' || user.role === 'Staff');
      if (!isStaffOrAdmin) {
        if (typeof window !== 'undefined') {
          window.location.href = '/login';
        }
      }
    }
  }, [user, isAuthLoading]);

  useEffect(() => {
    let mounted = true;
    async function loadAdminData() {
      try {
        const ords = await api.orders.getAll();
        if (mounted && Array.isArray(ords)) setOrders(ords);

        const prods = await api.products.getAll();
        if (mounted && Array.isArray(prods)) setProductsCount(prods.length);

        const usrs = await api.users.getAll();
        if (mounted && Array.isArray(usrs)) setUsersCount(usrs.length);
      } catch (err) {
        console.warn("Admin data load error:", err);
      } finally {
        if (mounted) setLoading(false);
      }
    }
    loadAdminData();
    return () => { mounted = false; };
  }, []);

  const fetchPendingStaff = async () => {
    if (!user || (user.roleId !== 1 && user.role !== 'Admin')) return;
    setLoadingPending(true);
    try {
      const data = await api.users.getPending();
      if (Array.isArray(data)) {
        setPendingStaff(data);
      }
    } catch (err) {
      console.warn("Failed to fetch pending staff", err);
    } finally {
      setLoadingPending(false);
    }
  };

  useEffect(() => {
    if (user && (user.roleId === 1 || user.role === 'Admin')) {
      fetchPendingStaff();
    }
  }, [user]);

  const handleApproveStaff = async (id, name) => {
    setActioningId(id);
    try {
      const res = await api.users.approve(id);
      if (res.success) {
        showToast(`Staff account "${name}" approved successfully!`);
        setPendingStaff(prev => prev.filter(u => u.userId !== id));
      } else {
        showToast(res.error || "Failed to approve staff account", "error");
      }
    } catch (err) {
      showToast(err.message || "Failed to approve staff account", "error");
    } finally {
      setActioningId(null);
    }
  };

  const handleRejectStaff = async (id, name) => {
    setActioningId(id);
    try {
      const res = await api.users.reject(id);
      if (res.success) {
        showToast(`Staff request for "${name}" rejected.`, "info");
        setPendingStaff(prev => prev.filter(u => u.userId !== id));
      } else {
        showToast(res.error || "Failed to reject staff request", "error");
      }
    } catch (err) {
      showToast(err.message || "Failed to reject staff request", "error");
    } finally {
      setActioningId(null);
    }
  };

  const totalRevenue = orders.reduce((sum, o) => sum + (Number(o.total) || 0), 0);

  const handleUpdateStatus = async (orderId, newStatus) => {
    try {
      await api.orders.update(orderId, { status: newStatus, total: orders.find(o => o.id === orderId)?.total || 100 });
      setOrders(orders.map(o => o.id === orderId ? { ...o, status: newStatus } : o));
      showToast(`Order #${orderId} marked as ${newStatus}`);
    } catch {
      setOrders(orders.map(o => o.id === orderId ? { ...o, status: newStatus } : o));
      showToast(`Status updated to ${newStatus}`);
    }
  };

  const openTrackingModal = async (order) => {
    setTrackingModalOrder(order);
    const initialStatus = order.status?.toLowerCase() || 'shipped';
    setTrackingStatus(initialStatus === 'delivered' ? 'delivered' : 'shipped');
    setTrackingNote('');
    setLoadingTracking(true);

    try {
      const res = await api.orders.getTracking(order.id);
      if (res && res.data) {
        setExistingTracking(res.data);
        if (res.data.checkpoints && res.data.checkpoints.length > 0) {
          const last = res.data.checkpoints[res.data.checkpoints.length - 1];
          setPickedCoords({ lat: last.latitude, lng: last.longitude });
        } else if (res.data.storeLocation?.latitude) {
          setPickedCoords({ lat: res.data.storeLocation.latitude, lng: res.data.storeLocation.longitude });
        } else {
          setPickedCoords({ lat: 40.7306, lng: -73.9352 });
        }
      } else {
        setPickedCoords({ lat: 40.7306, lng: -73.9352 });
      }
    } catch {
      setExistingTracking(null);
      setPickedCoords({ lat: 40.7306, lng: -73.9352 });
    } finally {
      setLoadingTracking(false);
    }
  };

  const handleAddCheckpoint = async (e) => {
    e.preventDefault();
    if (!trackingModalOrder) return;
    setSubmittingTracking(true);

    try {
      const payload = {
        status: trackingStatus,
        latitude: parseFloat(pickedCoords.lat),
        longitude: parseFloat(pickedCoords.lng),
        note: trackingNote.trim() || `Package status updated to ${trackingStatus}`,
      };

      const res = await api.orders.addTracking(trackingModalOrder.id, payload);
      if (res.success || !res.error) {
        setOrders(orders.map(o => o.id === trackingModalOrder.id ? { ...o, status: trackingStatus } : o));
        showToast(`Tracking checkpoint recorded for Order #${trackingModalOrder.id}!`);
        setTrackingModalOrder(null);
      } else {
        showToast(res.error || "Failed to add tracking checkpoint", "error");
      }
    } catch (err) {
      console.warn("Checkpoint submit error:", err);
      showToast("Error updating tracking checkpoint", "error");
    } finally {
      setSubmittingTracking(false);
    }
  };

  const stats = [
    { label: "Total Products", value: productsCount.toString(), change: "+3 this week", icon: Package },
    { label: "Total Orders", value: (orders.length || 5).toString(), change: "+8% from last week", icon: ShoppingBag },
    { label: "Active Customers", value: (usersCount || 3).toString(), change: "+2 registered today", icon: Users },
    { label: "Gross Revenue", value: `₱${totalRevenue.toFixed(2)}`, change: "+14.2% growth", icon: BarChart2 },
  ];

  if (!isAuthLoading && !canManageProducts) {
    return (
      <div className="min-h-[calc(100vh-40px)] bg-gray-50 flex items-center justify-center p-6">
        <div className="max-w-md w-full bg-white rounded-2xl border border-gray-200 p-8 shadow-sm text-center animate-in fade-in zoom-in-95">
          <div className="w-14 h-14 mx-auto rounded-2xl bg-amber-50 border border-amber-200 flex items-center justify-center text-amber-600 mb-5">
            <Shield size={28} />
          </div>
          <h2 className="text-xl font-bold text-gray-900 tracking-tight">Access Restricted</h2>
          <p className="text-sm text-gray-500 mt-2 leading-relaxed">
            The Management Dashboard is restricted to verified <strong>Staff</strong> and <strong>Administrators</strong>.
          </p>

          <div className="my-6 p-3.5 bg-gray-50 rounded-xl border border-gray-200 text-xs text-gray-600 flex items-center justify-between">
            <span className="text-gray-400">Current Account:</span>
            <span className="font-semibold text-gray-800">
              {user ? (
                <span>
                  {user.roleId === 1 ? '👑 Administrator' : user.roleId === 2 ? '🛡️ Staff' : '👤 Customer'} ({user.username})
                </span>
              ) : (
                'Not Signed In'
              )}
            </span>
          </div>

          <div className="flex flex-col sm:flex-row gap-2.5">
            <button
              onClick={() => navigateTo('home')}
              className="flex-1 px-4 py-2.5 bg-gray-900 hover:bg-gray-800 text-white text-xs font-semibold rounded-xl transition cursor-pointer"
            >
              Back to Storefront
            </button>
            <button
              onClick={() => navigateTo('login')}
              className="px-4 py-2.5 bg-gray-100 hover:bg-gray-200 text-gray-800 text-xs font-semibold rounded-xl border border-gray-200 transition cursor-pointer"
            >
              Sign In as Staff / Admin
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="flex min-h-[calc(100vh-40px)] bg-gray-100">
      
      {/* Admin Sidebar */}
      <aside className="w-56 bg-gray-900 min-h-full flex-shrink-0 flex flex-col text-neutral-300">
        <div className="px-5 py-5 border-b border-gray-800">
          <p className="text-[10px] font-black text-gray-400 uppercase tracking-widest">STYLESHOP</p>
          <p className="text-sm font-bold text-white mt-0.5">Staff & Admin Portal</p>
        </div>

        <nav className="flex-1 px-3 py-4 space-y-1">
          <button
            onClick={() => setCurrentSection('overview')}
            className={`w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-xs font-semibold transition cursor-pointer ${
              currentSection === 'overview'
                ? 'bg-gray-800 text-white shadow-xs'
                : 'text-gray-400 hover:text-white hover:bg-gray-800'
            }`}
          >
            <BarChart2 size={16} />
            <span>Dashboard</span>
          </button>

          {/* Staff Requests section - only visible to Admins (roleId === 1) */}
          {user && (user.roleId === 1 || user.role === 'Admin') && (
            <button
              onClick={() => {
                setCurrentSection('staff-requests');
                fetchPendingStaff();
              }}
              className={`w-full flex items-center justify-between px-3 py-2.5 rounded-lg text-xs font-semibold transition cursor-pointer ${
                currentSection === 'staff-requests'
                  ? 'bg-gray-800 text-white shadow-xs'
                  : 'text-gray-400 hover:text-white hover:bg-gray-800'
              }`}
            >
              <div className="flex items-center gap-3">
                <UserCheck size={16} />
                <span>Staff Requests</span>
              </div>
              {pendingStaff.length > 0 && (
                <span className="px-1.5 py-0.5 rounded-full text-[10px] font-bold bg-amber-500 text-neutral-950">
                  {pendingStaff.length}
                </span>
              )}
            </button>
          )}

          <button
            onClick={() => navigateTo('products')}
            className="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-xs font-semibold text-gray-400 hover:text-white hover:bg-gray-800 transition cursor-pointer"
          >
            <Package size={16} />
            <span>Product Catalog</span>
          </button>
          <button
            onClick={() => navigateTo('home')}
            className="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-xs font-semibold text-gray-400 hover:text-white hover:bg-gray-800 transition cursor-pointer"
          >
            <Store size={16} />
            <span>Back to Storefront</span>
          </button>
        </nav>

        <div className="p-3 border-t border-gray-800">
          {user && (
            <div className="px-3 py-2 text-[11px] text-gray-400 mb-1">
              <span className="text-gray-500 block text-[10px] uppercase font-bold tracking-wider">Signed in as:</span>
              <span className="text-neutral-200 font-semibold">{user.roleId === 1 ? '👑 Admin' : '🛡️ Staff'} ({user.username})</span>
            </div>
          )}
          <button
            onClick={logout}
            className="w-full flex items-center gap-3 px-3 py-2 rounded-lg text-xs font-semibold text-gray-400 hover:text-red-400 hover:bg-gray-800 transition cursor-pointer"
          >
            <LogOut size={15} />
            <span>Sign Out</span>
          </button>
        </div>
      </aside>

      {/* Main Admin Area */}
      <main className="flex-1 overflow-auto">
        
        {/* Top Bar */}
        <div className="px-8 py-5 border-b border-gray-200 bg-white flex items-center justify-between">
          <div>
            <h1 className="text-lg font-black text-gray-900 tracking-tight">
              {currentSection === 'staff-requests' && user && (user.roleId === 1 || user.role === 'Admin')
                ? 'Staff Access Requests'
                : 'Overview Dashboard'}
            </h1>
            <p className="text-xs text-gray-500 mt-0.5">
              {currentSection === 'staff-requests' && user && (user.roleId === 1 || user.role === 'Admin')
                ? 'Review and authorize pending staff account sign-up requests'
                : 'Real-time telemetry and API database synchronization'}
            </p>
          </div>
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 bg-gray-900 text-white font-bold rounded-full flex items-center justify-center text-xs">
              AD
            </div>
            <div>
              <p className="text-xs font-bold text-gray-800">{user?.username || 'admin'}</p>
              <p className="text-[10px] text-gray-400">{user?.email || 'admin@styleshop.com'}</p>
            </div>
          </div>
        </div>

        <div className="p-8 space-y-6">
          {currentSection === 'staff-requests' && user && (user.roleId === 1 || user.role === 'Admin') ? (
            <div className="space-y-6">
              {/* Header Card */}
              <div className="bg-white border border-gray-200 rounded-xl p-6 shadow-2xs flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
                <div>
                  <h2 className="text-base font-bold text-gray-900 tracking-tight">Pending Staff Account Requests</h2>
                  <p className="text-xs text-gray-500 mt-1">
                    The following staff members have submitted registration requests using an authorized invite code.
                  </p>
                </div>
                <button
                  onClick={fetchPendingStaff}
                  disabled={loadingPending}
                  className="inline-flex items-center gap-1.5 px-3.5 py-2 text-xs font-semibold text-gray-700 bg-gray-100 hover:bg-gray-200 rounded-lg transition cursor-pointer self-start sm:self-auto"
                >
                  <RefreshCw size={13} className={loadingPending ? "animate-spin" : ""} />
                  <span>Refresh Requests</span>
                </button>
              </div>

              {/* Table or Empty State */}
              <div className="bg-white border border-gray-200 rounded-xl overflow-hidden shadow-2xs">
                {loadingPending ? (
                  <div className="p-12 text-center text-xs text-gray-500 flex flex-col items-center gap-2">
                    <RefreshCw size={20} className="animate-spin text-gray-400" />
                    <span>Loading pending staff requests...</span>
                  </div>
                ) : pendingStaff.length === 0 ? (
                  <div className="p-12 text-center">
                    <div className="w-12 h-12 rounded-2xl bg-emerald-50 text-emerald-600 flex items-center justify-center mx-auto mb-3">
                      <CheckCircle size={24} />
                    </div>
                    <p className="text-sm font-bold text-gray-900">No Pending Requests</p>
                    <p className="text-xs text-gray-500 mt-1 max-w-sm mx-auto">
                      All staff registrations have been reviewed. New requests submitted with a staff invite code will appear here for admin authorization.
                    </p>
                  </div>
                ) : (
                  <table className="w-full text-left text-xs">
                    <thead>
                      <tr className="border-b border-gray-100 bg-gray-50/80 text-[10px] font-bold text-gray-500 uppercase tracking-wider">
                        <th className="px-5 py-3">ID</th>
                        <th className="px-4 py-3">Staff Username</th>
                        <th className="px-4 py-3">Email Address</th>
                        <th className="px-4 py-3">Requested Role</th>
                        <th className="px-4 py-3">Account Status</th>
                        <th className="px-4 py-3 text-right">Actions</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-100">
                      {pendingStaff.map((s) => (
                        <tr key={s.userId} className="hover:bg-gray-50/60 transition">
                          <td className="px-5 py-3.5 font-mono text-gray-400">
                            #{s.userId}
                          </td>
                          <td className="px-4 py-3.5 font-bold text-gray-900">
                            {s.userName}
                          </td>
                          <td className="px-4 py-3.5 text-gray-600">
                            {s.email}
                          </td>
                          <td className="px-4 py-3.5">
                            <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-neutral-100 text-neutral-800 border border-neutral-200">
                              Staff (Role 2)
                            </span>
                          </td>
                          <td className="px-4 py-3.5">
                            <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-amber-100 text-amber-800 border border-amber-200">
                              Pending Approval
                            </span>
                          </td>
                          <td className="px-4 py-3.5 text-right">
                            <div className="flex items-center justify-end gap-2">
                              <button
                                onClick={() => handleApproveStaff(s.userId, s.userName)}
                                disabled={actioningId === s.userId}
                                className="px-3 py-1.5 text-xs font-semibold bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg transition shadow-xs flex items-center gap-1.5 cursor-pointer disabled:opacity-50"
                              >
                                <Check size={13} />
                                <span>Approve</span>
                              </button>
                              <button
                                onClick={() => handleRejectStaff(s.userId, s.userName)}
                                disabled={actioningId === s.userId}
                                className="px-3 py-1.5 text-xs font-semibold bg-red-600 hover:bg-red-700 text-white rounded-lg transition shadow-xs flex items-center gap-1.5 cursor-pointer disabled:opacity-50"
                              >
                                <X size={13} />
                                <span>Reject</span>
                              </button>
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </div>
            </div>
          ) : (
            <>
              {/* KPI Stat Cards */}
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
                {stats.map(({ label, value, change, icon: Icon }) => (
                  <div key={label} className="bg-white border border-gray-200 rounded-xl p-5 shadow-2xs">
                    <div className="flex items-center justify-between mb-3">
                      <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide">{label}</p>
                      <div className="w-8 h-8 bg-gray-100 rounded-lg flex items-center justify-center text-gray-700">
                        <Icon size={16} />
                      </div>
                    </div>
                    <p className="text-2xl font-black text-gray-900">{value}</p>
                    <p className="text-[11px] text-emerald-600 font-semibold mt-1 flex items-center gap-1">
                      <TrendingUp size={12} />
                      <span>{change}</span>
                    </p>
                  </div>
                ))}
              </div>

              {/* Visual Performance Charts (SVG based) */}
              <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">
                
                {/* Revenue Histogram Chart */}
                <div className="lg:col-span-2 bg-white border border-gray-200 rounded-xl p-5 shadow-2xs space-y-3">
                  <div className="flex justify-between items-center">
                    <h3 className="text-xs font-bold text-gray-900 uppercase tracking-wider">Revenue Breakdown (Monthly)</h3>
                    <span className="text-xs font-bold text-gray-600 bg-gray-100 px-2 py-0.5 rounded">2025 - 2026</span>
                  </div>

                  <div className="h-44 pt-4 flex items-end justify-between gap-3 border-b border-gray-100 pb-2">
                    {[
                      { month: 'Jan', val: 40 },
                      { month: 'Feb', val: 55 },
                      { month: 'Mar', val: 70 },
                      { month: 'Apr', val: 65 },
                      { month: 'May', val: 85 },
                      { month: 'Jun', val: 95 },
                      { month: 'Jul', val: 80 },
                      { month: 'Aug', val: 90 },
                      { month: 'Sep', val: 100 },
                    ].map((bar) => (
                      <div key={bar.month} className="flex-1 flex flex-col items-center gap-1.5 h-full justify-end group">
                        <div
                          style={{ height: `${bar.val}%` }}
                          className="w-full bg-gray-800 hover:bg-gray-600 rounded-t transition-all relative"
                        >
                          <span className="opacity-0 group-hover:opacity-100 transition absolute -top-6 left-1/2 -translate-x-1/2 bg-gray-900 text-white text-[10px] px-1.5 py-0.5 rounded">
                            ${bar.val * 35}
                          </span>
                        </div>
                        <span className="text-[10px] text-gray-400 font-medium">{bar.month}</span>
                      </div>
                    ))}
                  </div>
                </div>

                {/* Category Share Chart */}
                <div className="bg-white border border-gray-200 rounded-xl p-5 shadow-2xs space-y-4">
                  <h3 className="text-xs font-bold text-gray-900 uppercase tracking-wider">Sales by Category</h3>
                  
                  <div className="space-y-3 pt-2">
                    {[
                      { cat: 'Tops & Shirts', pct: 45, color: 'bg-gray-900' },
                      { cat: 'Bottoms & Denim', pct: 30, color: 'bg-gray-600' },
                      { cat: 'Shoes & Footwear', pct: 15, color: 'bg-gray-400' },
                      { cat: 'Accessories', pct: 10, color: 'bg-gray-300' },
                    ].map((item) => (
                      <div key={item.cat} className="space-y-1">
                        <div className="flex justify-between text-xs">
                          <span className="font-semibold text-gray-700">{item.cat}</span>
                          <span className="text-gray-500 font-mono">{item.pct}%</span>
                        </div>
                        <div className="w-full h-2 bg-gray-100 rounded-full overflow-hidden">
                          <div style={{ width: `${item.pct}%` }} className={`h-full ${item.color} rounded-full`} />
                        </div>
                      </div>
                    ))}
                  </div>
                </div>

              </div>

              {/* Recent Orders Table with Status Actions */}
              <div className="bg-white border border-gray-200 rounded-xl overflow-hidden shadow-2xs">
                <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between">
                  <div>
                    <h3 className="text-sm font-bold text-gray-900 uppercase tracking-wider">Recent Orders</h3>
                    <p className="text-xs text-gray-400 mt-0.5">Manage fulfillment statuses and customer deliveries</p>
                  </div>
                  <button
                    onClick={() => navigateTo('products')}
                    className="text-xs font-semibold text-gray-600 hover:text-gray-900 underline"
                  >
                    Go to Products →
                  </button>
                </div>

                <table className="w-full text-left text-xs">
                  <thead>
                    <tr className="border-b border-gray-100 bg-gray-50/80 text-[10px] font-bold text-gray-500 uppercase tracking-wider">
                      <th className="px-5 py-3">Order ID</th>
                      <th className="px-4 py-3">Customer ID</th>
                      <th className="px-4 py-3">Date</th>
                      <th className="px-4 py-3">Total</th>
                      <th className="px-4 py-3">Status</th>
                      <th className="px-4 py-3 text-right">Update Status</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100">
                    {orders.map((o) => (
                      <tr key={o.id} className="hover:bg-gray-50/60 transition">
                        <td className="px-5 py-3.5 font-bold font-mono text-gray-900">
                          #ORD-{o.id}
                        </td>
                        <td className="px-4 py-3.5 text-gray-600">
                          User #{o.userId}
                        </td>
                        <td className="px-4 py-3.5 text-gray-500">
                          {o.orderDate ? new Date(o.orderDate).toLocaleDateString() : 'Jul 12, 2025'}
                        </td>
                        <td className="px-4 py-3.5 font-bold text-gray-900">
                          ₱{Number(o.total).toFixed(2)}
                        </td>
                        <td className="px-4 py-3.5">
                          <span
                            className={`px-2 py-0.5 rounded text-[10px] font-bold ${
                              o.status === 'Delivered'
                                ? 'bg-emerald-100 text-emerald-800'
                                : o.status === 'Shipped'
                                ? 'bg-blue-100 text-blue-800'
                                : o.status === 'Cancelled'
                                ? 'bg-red-100 text-red-800'
                                : 'bg-amber-100 text-amber-800'
                            }`}
                          >
                            {o.status || 'Processing'}
                          </span>
                        </td>
                        <td className="px-4 py-3.5 text-right flex items-center justify-end gap-2">
                          <select
                            value={o.status || 'Processing'}
                            onChange={(e) => handleUpdateStatus(o.id, e.target.value)}
                            className="px-2 py-1 text-xs border border-gray-300 rounded bg-white text-gray-700 cursor-pointer"
                          >
                            <option value="Processing">Processing</option>
                            <option value="Shipped">Shipped</option>
                            <option value="Delivered">Delivered</option>
                            <option value="Cancelled">Cancelled</option>
                          </select>
                          <button
                            onClick={() => openTrackingModal(o)}
                            className="px-2 py-1 text-xs font-semibold bg-blue-50 hover:bg-blue-100 text-blue-700 border border-blue-200 rounded transition cursor-pointer flex items-center gap-1"
                            title="Update Checkpoint on Map"
                          >
                            <MapPin size={12} />
                            <span>Tracking</span>
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </>
          )}
        </div>

      </main>

      {/* Admin Update Tracking & Checkpoint Modal */}
      {trackingModalOrder && (
        <div className="fixed inset-0 bg-black/60 z-50 flex items-center justify-center p-4 overflow-y-auto">
          <div className="bg-white rounded-2xl max-w-2xl w-full p-6 space-y-4 shadow-2xl border border-gray-200 animate-in fade-in zoom-in-95 my-8">
            <div className="flex items-center justify-between pb-3 border-b border-gray-100">
              <div className="flex items-center gap-2">
                <div className="w-8 h-8 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center">
                  <MapPin size={18} />
                </div>
                <div>
                  <h3 className="text-sm font-bold text-gray-900">
                    Update Order Tracking Telemetry
                  </h3>
                  <p className="text-xs text-gray-400">
                    Order #ORD-{trackingModalOrder.id} • Customer #{trackingModalOrder.userId}
                  </p>
                </div>
              </div>
              <button
                onClick={() => setTrackingModalOrder(null)}
                className="p-1.5 rounded-lg text-gray-400 hover:text-gray-700 hover:bg-gray-100 transition"
              >
                <X size={16} />
              </button>
            </div>

            <form onSubmit={handleAddCheckpoint} className="space-y-4">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-bold text-gray-700 mb-1">
                    Checkpoint Status <span className="text-red-500">*</span>
                  </label>
                  <select
                    value={trackingStatus}
                    onChange={(e) => setTrackingStatus(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-xl text-xs bg-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                  >
                    <option value="pending">pending (Order Placed)</option>
                    <option value="packed">packed (Quality Checked & Packed)</option>
                    <option value="shipped">shipped (Handed to Carrier)</option>
                    <option value="out_for_delivery">out_for_delivery (Out with Courier)</option>
                    <option value="delivered">delivered (Successfully Delivered)</option>
                    <option value="cancelled">cancelled (Order Cancelled)</option>
                  </select>
                </div>

                <div>
                  <label className="block text-xs font-bold text-gray-700 mb-1">
                    Checkpoint Note / Message
                  </label>
                  <input
                    type="text"
                    placeholder="e.g. In transit through Queens distribution hub"
                    value={trackingNote}
                    onChange={(e) => setTrackingNote(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-xl text-xs focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
              </div>

              {/* Interactive Map Picker */}
              <div>
                <div className="flex items-center justify-between mb-1.5">
                  <label className="block text-xs font-bold text-gray-700">
                    Pin Location on Map <span className="text-gray-400 font-normal">(Click anywhere on the map to set coordinates)</span>
                  </label>
                  <span className="text-[11px] font-mono font-semibold text-blue-600 bg-blue-50 px-2 py-0.5 rounded border border-blue-100">
                    {pickedCoords.lat.toFixed(5)}, {pickedCoords.lng.toFixed(5)}
                  </span>
                </div>

                <TrackingMap
                  storeLocation={existingTracking?.storeLocation}
                  deliveryAddress={existingTracking?.deliveryAddress}
                  checkpoints={existingTracking?.checkpoints || []}
                  interactivePick={true}
                  pickedLocation={pickedCoords}
                  onPickLocation={(lat, lng) => setPickedCoords({ lat, lng })}
                  height="260px"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-[11px] font-semibold text-gray-500 mb-1">
                    Latitude (-90 to 90)
                  </label>
                  <input
                    type="number"
                    step="any"
                    value={pickedCoords.lat}
                    onChange={(e) => setPickedCoords({ ...pickedCoords, lat: parseFloat(e.target.value) || 0 })}
                    className="w-full px-3 py-1.5 border border-gray-300 rounded-lg text-xs font-mono"
                    required
                  />
                </div>
                <div>
                  <label className="block text-[11px] font-semibold text-gray-500 mb-1">
                    Longitude (-180 to 180)
                  </label>
                  <input
                    type="number"
                    step="any"
                    value={pickedCoords.lng}
                    onChange={(e) => setPickedCoords({ ...pickedCoords, lng: parseFloat(e.target.value) || 0 })}
                    className="w-full px-3 py-1.5 border border-gray-300 rounded-lg text-xs font-mono"
                    required
                  />
                </div>
              </div>

              <div className="flex items-center justify-between pt-3 border-t border-gray-100">
                <button
                  type="button"
                  onClick={() => {
                    const id = trackingModalOrder.id;
                    setTrackingModalOrder(null);
                    navigateTo('tracking', { orderId: id });
                  }}
                  className="px-3 py-1.5 text-xs text-blue-600 hover:text-blue-800 hover:underline flex items-center gap-1 font-semibold"
                >
                  <Navigation size={13} />
                  <span>View Customer Tracking Screen</span>
                </button>

                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    onClick={() => setTrackingModalOrder(null)}
                    className="px-3.5 py-1.5 text-xs font-semibold text-gray-600 hover:bg-gray-100 rounded-lg transition"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    disabled={submittingTracking}
                    className="px-4 py-2 text-xs font-semibold bg-gray-900 hover:bg-gray-800 text-white rounded-xl shadow-xs transition flex items-center gap-1.5 cursor-pointer disabled:opacity-50"
                  >
                    {submittingTracking ? (
                      <>
                        <RefreshCw size={13} className="animate-spin" />
                        <span>Saving...</span>
                      </>
                    ) : (
                      <>
                        <CheckCircle size={14} />
                        <span>Save Checkpoint & Push Update</span>
                      </>
                    )}
                  </button>
                </div>
              </div>
            </form>
          </div>
        </div>
      )}

    </div>
  );
}
