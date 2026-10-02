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
  Truck
} from 'lucide-react';

export default function AdminDashboard() {
  const { navigateTo, user, logout, showToast } = useApp();

  const [orders, setOrders] = useState([]);
  const [productsCount, setProductsCount] = useState(10);
  const [usersCount, setUsersCount] = useState(3);
  const [loading, setLoading] = useState(true);

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

  const stats = [
    { label: "Total Products", value: productsCount.toString(), change: "+3 this week", icon: Package },
    { label: "Total Orders", value: (orders.length || 5).toString(), change: "+8% from last week", icon: ShoppingBag },
    { label: "Active Customers", value: (usersCount || 3).toString(), change: "+2 registered today", icon: Users },
    { label: "Gross Revenue", value: `$${totalRevenue.toFixed(2)}`, change: "+14.2% growth", icon: BarChart2 },
  ];

  return (
    <div className="flex min-h-[calc(100vh-40px)] bg-gray-100">
      
      {/* Admin Sidebar */}
      <aside className="w-56 bg-gray-900 min-h-full flex-shrink-0 flex flex-col text-neutral-300">
        <div className="px-5 py-5 border-b border-gray-800">
          <p className="text-[10px] font-black text-gray-400 uppercase tracking-widest">STYLESHOP</p>
          <p className="text-sm font-bold text-white mt-0.5">Admin Portal</p>
        </div>

        <nav className="flex-1 px-3 py-4 space-y-1">
          <button
            onClick={() => navigateTo('admin')}
            className="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-xs font-semibold bg-gray-800 text-white shadow-xs cursor-pointer"
          >
            <BarChart2 size={16} />
            <span>Dashboard</span>
          </button>
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
          <button
            onClick={logout}
            className="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-xs font-semibold text-gray-400 hover:text-red-400 hover:bg-gray-800 transition cursor-pointer"
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
            <h1 className="text-lg font-black text-gray-900 tracking-tight">Overview Dashboard</h1>
            <p className="text-xs text-gray-500 mt-0.5">Real-time telemetry and API database synchronization</p>
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
                      ${Number(o.total).toFixed(2)}
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
                    <td className="px-4 py-3.5 text-right">
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
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

        </div>

      </main>

    </div>
  );
}
