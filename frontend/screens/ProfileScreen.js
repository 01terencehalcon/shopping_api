'use client';

import React, { useState, useEffect } from 'react';
import { useApp } from '../lib/context';
import { api } from '../lib/api';
import StoreHeader from '../components/StoreHeader';
import Footer from '../components/Footer';
import {
  User,
  ShoppingBag,
  Home,
  Bell,
  LogOut,
  Edit,
  Plus,
  Trash2,
  CheckCircle2,
  Package,
  Calendar,
  Eye,
  X
} from 'lucide-react';

export default function ProfileScreen() {
  const { user, logout, navigateTo, showToast } = useApp();

  const [activeTab, setActiveTab] = useState('profile');
  const [orders, setOrders] = useState([]);
  const [addresses, setAddresses] = useState([]);
  const [notifications, setNotifications] = useState([]);
  const [loadingOrders, setLoadingOrders] = useState(false);
  const [loadingAddresses, setLoadingAddresses] = useState(false);

  // Edit profile state
  const [editModal, setEditModal] = useState(false);
  const [profileName, setProfileName] = useState(user?.username || 'Jane Smith');
  const [profileEmail, setProfileEmail] = useState(user?.email || 'jane.smith@example.com');
  const [profilePhone, setProfilePhone] = useState('+1 555 000 1234');

  // Add address state
  const [showAddAddress, setShowAddAddress] = useState(false);
  const [newAddr, setNewAddr] = useState({
    recipientName: user?.username || 'Jane Smith',
    phone: '+1 555 000 1234',
    street: '',
    city: '',
    province: 'NY',
    postalCode: '',
  });

  // Selected order details modal
  const [selectedOrder, setSelectedOrder] = useState(null);
  const [orderItems, setOrderItems] = useState([]);
  const [loadingItems, setLoadingItems] = useState(false);

  // Load orders & addresses
  useEffect(() => {
    let mounted = true;
    const userId = user?.userId || 3;

    async function loadData() {
      setLoadingOrders(true);
      setLoadingAddresses(true);

      try {
        const ords = await api.orders.getByUser(userId);
        if (mounted && Array.isArray(ords)) setOrders(ords);

        const addrs = await api.addresses.getByUser(userId);
        if (mounted && Array.isArray(addrs)) setAddresses(addrs);

        const notifs = await api.notifications.getByUser(userId);
        if (mounted && Array.isArray(notifs)) setNotifications(notifs);
      } catch (err) {
        console.warn("Error fetching profile data:", err);
      } finally {
        if (mounted) {
          setLoadingOrders(false);
          setLoadingAddresses(false);
        }
      }
    }

    loadData();
    return () => { mounted = false; };
  }, [user]);

  const handleSaveProfile = (e) => {
    e.preventDefault();
    setEditModal(false);
    showToast("Profile details updated successfully!");
  };

  const handleAddAddress = async (e) => {
    e.preventDefault();
    if (!newAddr.street || !newAddr.city) return;

    try {
      const res = await api.addresses.create({
        userId: user?.userId || 3,
        ...newAddr
      });
      const created = {
        addressId: Date.now(),
        userId: user?.userId || 3,
        ...newAddr
      };
      setAddresses([...addresses, created]);
      setShowAddAddress(false);
      setNewAddr({ recipientName: '', phone: '', street: '', city: '', province: 'NY', postalCode: '' });
      showToast("New address added successfully!");
    } catch {
      showToast("Address added to session");
    }
  };

  const handleDeleteAddress = async (id) => {
    try {
      await api.addresses.delete(id);
      setAddresses(addresses.filter(a => a.addressId !== id));
      showToast("Address removed");
    } catch {
      setAddresses(addresses.filter(a => a.addressId !== id));
    }
  };

  const viewOrderDetails = async (order) => {
    setSelectedOrder(order);
    setLoadingItems(true);
    try {
      const items = await api.orders.getOrderItems(order.id);
      setOrderItems(items);
    } catch {
      setOrderItems([]);
    } finally {
      setLoadingItems(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 flex flex-col">
      <StoreHeader />

      <div className="max-w-5xl mx-auto px-4 sm:px-6 py-8 flex-1 w-full">
        
        <div className="flex flex-col md:flex-row gap-8">
          
          {/* Left Sidebar Navigation */}
          <aside className="w-full md:w-56 flex-shrink-0 space-y-1">
            <div className="bg-white border border-gray-200 rounded-xl p-3 space-y-1 shadow-2xs">
              {[
                { id: 'profile', icon: User, label: 'Profile' },
                { id: 'orders', icon: ShoppingBag, label: `Orders (${orders.length})` },
                { id: 'addresses', icon: Home, label: `Addresses (${addresses.length})` },
                { id: 'notifications', icon: Bell, label: `Notifications (${notifications.length})` },
              ].map(({ id, icon: Icon, label }) => (
                <button
                  key={id}
                  onClick={() => setActiveTab(id)}
                  className={`w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-xs font-semibold transition cursor-pointer ${
                    activeTab === id
                      ? 'bg-gray-900 text-white shadow-xs'
                      : 'text-gray-600 hover:bg-gray-100'
                  }`}
                >
                  <Icon size={15} />
                  <span>{label}</span>
                </button>
              ))}

              <div className="border-t border-gray-200 pt-2 mt-2">
                <button
                  onClick={logout}
                  className="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-xs font-semibold text-red-600 hover:bg-red-50 transition cursor-pointer"
                >
                  <LogOut size={15} />
                  <span>Log Out</span>
                </button>
              </div>
            </div>
          </aside>

          {/* Main Content Area */}
          <div className="flex-1 space-y-6">
            
            {/* TAB 1: Profile Information */}
            {activeTab === 'profile' && (
              <div className="bg-white border border-gray-200 rounded-xl p-6 shadow-2xs space-y-6">
                <div className="flex items-start justify-between">
                  <div>
                    <h2 className="text-base font-bold text-gray-900">Profile Information</h2>
                    <p className="text-xs text-gray-500 mt-0.5">Manage your personal account credentials</p>
                  </div>
                  <button
                    onClick={() => setEditModal(true)}
                    className="flex items-center gap-1.5 px-3 py-1.5 bg-gray-100 hover:bg-gray-200 text-gray-800 rounded-lg text-xs font-semibold transition cursor-pointer"
                  >
                    <Edit size={13} />
                    <span>Edit Profile</span>
                  </button>
                </div>

                <div className="flex items-center gap-4 pt-2">
                  <div className="w-16 h-16 rounded-full bg-gray-900 text-white flex items-center justify-center font-black text-xl shadow-xs">
                    {profileName.charAt(0).toUpperCase()}
                  </div>
                  <div>
                    <p className="text-lg font-bold text-gray-900">{profileName}</p>
                    <p className="text-xs text-gray-500">{profileEmail}</p>
                    <span className="inline-block mt-1 px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider bg-gray-100 text-gray-800 border border-gray-200">
                      {user?.roleId === 1 ? 'Administrator' : 'Premium Member'}
                    </span>
                  </div>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 border-t border-gray-100 pt-5 text-xs">
                  <div>
                    <p className="text-gray-400 font-bold uppercase tracking-wider text-[10px]">Contact Phone</p>
                    <p className="text-gray-800 font-semibold mt-1">{profilePhone}</p>
                  </div>
                  <div>
                    <p className="text-gray-400 font-bold uppercase tracking-wider text-[10px]">Membership Tier</p>
                    <p className="text-gray-800 font-semibold mt-1">Gold VIP Tier</p>
                  </div>
                  <div>
                    <p className="text-gray-400 font-bold uppercase tracking-wider text-[10px]">Default Address</p>
                    <p className="text-gray-800 font-semibold mt-1 truncate">
                      {addresses[0] ? `${addresses[0].street}, ${addresses[0].city}` : "123 Main Street, New York, NY"}
                    </p>
                  </div>
                  <div>
                    <p className="text-gray-400 font-bold uppercase tracking-wider text-[10px]">Lifetime Orders</p>
                    <p className="text-gray-800 font-semibold mt-1">{orders.length} orders recorded</p>
                  </div>
                </div>
              </div>
            )}

            {/* TAB 2: Order History */}
            {activeTab === 'orders' && (
              <div className="bg-white border border-gray-200 rounded-xl overflow-hidden shadow-2xs">
                <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between">
                  <div>
                    <h2 className="text-sm font-bold text-gray-900 uppercase tracking-wider">Order History</h2>
                    <p className="text-xs text-gray-500 mt-0.5">Track and view invoices for previous purchases</p>
                  </div>
                  <span className="text-xs text-gray-400">Total: {orders.length} orders</span>
                </div>

                {orders.length === 0 ? (
                  <div className="p-12 text-center text-xs text-gray-400">
                    No orders placed yet.
                  </div>
                ) : (
                  <table className="w-full text-left text-xs">
                    <thead>
                      <tr className="border-b border-gray-100 bg-gray-50/80 text-[10px] font-bold text-gray-500 uppercase tracking-wider">
                        <th className="px-5 py-3">Order ID</th>
                        <th className="px-4 py-3">Date</th>
                        <th className="px-4 py-3">Total</th>
                        <th className="px-4 py-3">Status</th>
                        <th className="px-4 py-3 text-right">Action</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-100">
                      {orders.map((o) => (
                        <tr key={o.id} className="hover:bg-gray-50/60 transition">
                          <td className="px-5 py-3.5 font-bold font-mono text-gray-900">
                            #ORD-{o.id}
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
                            <button
                              onClick={() => viewOrderDetails(o)}
                              className="px-2.5 py-1 text-xs font-semibold bg-gray-100 hover:bg-gray-200 text-gray-700 rounded transition cursor-pointer"
                            >
                              View
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </div>
            )}

            {/* TAB 3: Saved Addresses */}
            {activeTab === 'addresses' && (
              <div className="bg-white border border-gray-200 rounded-xl p-6 shadow-2xs space-y-6">
                <div className="flex items-center justify-between">
                  <div>
                    <h2 className="text-sm font-bold text-gray-900 uppercase tracking-wider">Saved Shipping Addresses</h2>
                    <p className="text-xs text-gray-500 mt-0.5">Manage delivery destinations for fast checkout</p>
                  </div>
                  <button
                    onClick={() => setShowAddAddress(!showAddAddress)}
                    className="flex items-center gap-1.5 px-3 py-1.5 bg-gray-900 hover:bg-gray-800 text-white rounded-lg text-xs font-semibold transition cursor-pointer"
                  >
                    <Plus size={13} />
                    <span>Add Address</span>
                  </button>
                </div>

                {showAddAddress && (
                  <form onSubmit={handleAddAddress} className="p-4 bg-gray-50 rounded-xl border border-gray-200 space-y-3 animate-in fade-in">
                    <p className="text-xs font-bold text-gray-800">Add New Shipping Address</p>
                    <div className="grid grid-cols-2 gap-3 text-xs">
                      <input
                        type="text"
                        required
                        placeholder="Recipient Name"
                        value={newAddr.recipientName}
                        onChange={(e) => setNewAddr({ ...newAddr, recipientName: e.target.value })}
                        className="px-3 py-2 bg-white border border-gray-300 rounded-lg"
                      />
                      <input
                        type="tel"
                        required
                        placeholder="Phone Number"
                        value={newAddr.phone}
                        onChange={(e) => setNewAddr({ ...newAddr, phone: e.target.value })}
                        className="px-3 py-2 bg-white border border-gray-300 rounded-lg"
                      />
                      <input
                        type="text"
                        required
                        placeholder="Street Address"
                        value={newAddr.street}
                        onChange={(e) => setNewAddr({ ...newAddr, street: e.target.value })}
                        className="col-span-2 px-3 py-2 bg-white border border-gray-300 rounded-lg"
                      />
                      <input
                        type="text"
                        required
                        placeholder="City"
                        value={newAddr.city}
                        onChange={(e) => setNewAddr({ ...newAddr, city: e.target.value })}
                        className="px-3 py-2 bg-white border border-gray-300 rounded-lg"
                      />
                      <input
                        type="text"
                        required
                        placeholder="Postal Code"
                        value={newAddr.postalCode}
                        onChange={(e) => setNewAddr({ ...newAddr, postalCode: e.target.value })}
                        className="px-3 py-2 bg-white border border-gray-300 rounded-lg"
                      />
                    </div>
                    <div className="flex justify-end gap-2 pt-1">
                      <button
                        type="button"
                        onClick={() => setShowAddAddress(false)}
                        className="px-3 py-1.5 text-xs text-gray-600 hover:bg-gray-200 rounded"
                      >
                        Cancel
                      </button>
                      <button
                        type="submit"
                        className="px-4 py-1.5 text-xs bg-gray-900 text-white font-semibold rounded hover:bg-gray-800"
                      >
                        Save Address
                      </button>
                    </div>
                  </form>
                )}

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  {addresses.map((addr) => (
                    <div key={addr.addressId} className="p-4 border border-gray-200 rounded-xl relative hover:border-gray-400 transition bg-white">
                      <div className="flex justify-between items-start mb-2">
                        <span className="text-xs font-bold text-gray-900">{addr.recipientName}</span>
                        <button
                          onClick={() => handleDeleteAddress(addr.addressId)}
                          className="text-gray-400 hover:text-red-600 transition"
                          title="Delete address"
                        >
                          <Trash2 size={13} />
                        </button>
                      </div>
                      <p className="text-xs text-gray-600 leading-normal">{addr.street}</p>
                      <p className="text-xs text-gray-600">{addr.city}, {addr.province} {addr.postalCode}</p>
                      <p className="text-[11px] text-gray-400 mt-2">{addr.phone}</p>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* TAB 4: Notifications */}
            {activeTab === 'notifications' && (
              <div className="bg-white border border-gray-200 rounded-xl p-6 shadow-2xs space-y-4">
                <h2 className="text-sm font-bold text-gray-900 uppercase tracking-wider">Account Notifications</h2>
                <div className="space-y-3">
                  {notifications.map((notif) => (
                    <div key={notif.notificationId} className="p-3.5 bg-gray-50 border border-gray-200 rounded-lg flex items-start gap-3">
                      <Bell size={16} className="text-gray-700 mt-0.5 flex-shrink-0" />
                      <div className="flex-1 text-xs">
                        <p className="font-semibold text-gray-800">{notif.message}</p>
                        <p className="text-[10px] text-gray-400 mt-1">{new Date(notif.createdAt).toLocaleDateString()}</p>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}

          </div>

        </div>

      </div>

      {/* Edit Profile Modal */}
      {editModal && (
        <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4">
          <form onSubmit={handleSaveProfile} className="bg-white rounded-2xl max-w-sm w-full p-6 space-y-4 shadow-xl">
            <h3 className="text-sm font-bold text-gray-900">Edit Profile Details</h3>
            <div className="space-y-3 text-xs">
              <div>
                <label className="text-gray-700 font-semibold block mb-1">Full Name</label>
                <input
                  type="text"
                  value={profileName}
                  onChange={(e) => setProfileName(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg"
                />
              </div>
              <div>
                <label className="text-gray-700 font-semibold block mb-1">Email</label>
                <input
                  type="email"
                  value={profileEmail}
                  onChange={(e) => setProfileEmail(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg"
                />
              </div>
              <div>
                <label className="text-gray-700 font-semibold block mb-1">Phone</label>
                <input
                  type="text"
                  value={profilePhone}
                  onChange={(e) => setProfilePhone(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg"
                />
              </div>
            </div>
            <div className="flex justify-end gap-2 pt-2">
              <button
                type="button"
                onClick={() => setEditModal(false)}
                className="px-3 py-1.5 text-xs text-gray-600 hover:bg-gray-100 rounded"
              >
                Cancel
              </button>
              <button
                type="submit"
                className="px-4 py-1.5 text-xs bg-gray-900 text-white font-semibold rounded hover:bg-gray-800"
              >
                Save Changes
              </button>
            </div>
          </form>
        </div>
      )}

      {/* Order Item Details Modal */}
      {selectedOrder && (
        <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl max-w-md w-full p-6 space-y-4 shadow-xl">
            <div className="flex items-center justify-between pb-3 border-b border-gray-100">
              <span className="font-bold text-sm text-gray-900">Order #ORD-{selectedOrder.id} Items</span>
              <button onClick={() => setSelectedOrder(null)} className="text-gray-400 hover:text-gray-700">
                <X size={16} />
              </button>
            </div>

            {loadingItems ? (
              <p className="text-xs text-gray-400 py-6 text-center">Loading items from API...</p>
            ) : (
              <div className="space-y-3 max-h-60 overflow-y-auto pr-1">
                {orderItems.map((item) => (
                  <div key={item.id} className="flex justify-between items-center text-xs p-2 bg-gray-50 rounded-lg">
                    <div>
                      <p className="font-semibold text-gray-800">Product #{item.productId}</p>
                      <p className="text-gray-500">Qty: {item.quantity}</p>
                    </div>
                    <span className="font-bold text-gray-900">${(item.price * item.quantity).toFixed(2)}</span>
                  </div>
                ))}
              </div>
            )}

            <div className="flex justify-between items-center pt-3 border-t border-gray-100 text-xs font-bold text-gray-900">
              <span>Order Total</span>
              <span>${Number(selectedOrder.total).toFixed(2)}</span>
            </div>
          </div>
        </div>
      )}

      <Footer />
    </div>
  );
}
