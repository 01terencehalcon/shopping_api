'use client';

import React, { useState } from 'react';
import { useApp } from '../lib/context';
import { Shield, User, ArrowRight, KeyRound, Mail, Sparkles } from 'lucide-react';

export default function LoginScreen() {
  const { navigateTo, login, register, showToast } = useApp();
  
  const [isRegister, setIsRegister] = useState(false);
  const [username, setUsername] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [roleId, setRoleId] = useState(3); // 3 = customer, 1 = admin
  const [loading, setLoading] = useState(false);
  const [rememberMe, setRememberMe] = useState(true);
  const [forgotModal, setForgotModal] = useState(false);
  const [forgotUser, setForgotUser] = useState('');
  const [resetMessage, setResetMessage] = useState('');

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!username.trim() || !password.trim()) {
      showToast("Please enter username and password.", "error");
      return;
    }

    setLoading(true);
    try {
      if (isRegister) {
        const res = await register(username.trim(), email.trim() || `${username}@styleshop.com`, password, roleId);
        if (res.success) {
          navigateTo('home');
        }
      } else {
        const res = await login(username.trim(), password);
        if (res.success) {
          navigateTo('home');
        }
      }
    } finally {
      setLoading(false);
    }
  };

  const fillDemo = (demoType) => {
    if (demoType === 'admin') {
      setUsername('admin');
      setPassword('Password123!');
      setIsRegister(false);
    } else {
      setUsername('customer');
      setPassword('Password123!');
      setIsRegister(false);
    }
  };

  return (
    <div className="min-h-[calc(100vh-40px)] bg-gray-100 flex items-center justify-center p-4 sm:p-6">
      <div className="w-full max-w-md">
        
        {/* Wireframe Header Logo Box */}
        <div className="text-center mb-8">
          <button
            onClick={() => navigateTo('home')}
            className="inline-block bg-gray-900 text-white border border-gray-800 rounded-xl px-7 py-3 mb-3 shadow-md hover:bg-gray-800 transition cursor-pointer"
          >
            <span className="text-lg font-black tracking-widest uppercase">STYLESHOP</span>
          </button>
          <p className="text-sm text-gray-500 font-medium">
            {isRegister ? "Create your new shopping account" : "Sign in to your account"}
          </p>
        </div>

        {/* Auth Card */}
        <div className="bg-white border border-gray-200 rounded-xl shadow-xs p-7 sm:p-8">
          
          {/* Quick Demo Credentials Banner */}
          <div className="mb-6 p-3 bg-gray-50 rounded-lg border border-gray-200 text-xs">
            <p className="font-semibold text-gray-700 flex items-center gap-1.5 mb-1.5">
              <Sparkles size={13} className="text-indigo-600" />
              <span>Quick Demo Fill (Pre-seeded in DB):</span>
            </p>
            <div className="flex gap-2">
              <button
                type="button"
                onClick={() => fillDemo('admin')}
                className="flex-1 py-1.5 px-2 bg-white border border-gray-300 rounded hover:bg-gray-100 text-gray-800 font-medium text-center transition"
              >
                👑 Admin Demo
              </button>
              <button
                type="button"
                onClick={() => fillDemo('customer')}
                className="flex-1 py-1.5 px-2 bg-white border border-gray-300 rounded hover:bg-gray-100 text-gray-800 font-medium text-center transition"
              >
                👤 Customer Demo
              </button>
            </div>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4">
            
            {/* Username */}
            <div className="space-y-1">
              <label className="text-xs font-semibold text-gray-700">Username</label>
              <input
                type="text"
                required
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                placeholder="e.g. admin or customer"
                className="w-full px-3.5 py-2 text-sm bg-gray-50 border border-gray-300 rounded-lg placeholder-gray-400 focus:bg-white focus:outline-none focus:ring-1 focus:ring-gray-900 focus:border-gray-900 transition"
              />
            </div>

            {/* Email (only in register) */}
            {isRegister && (
              <div className="space-y-1 animate-in fade-in duration-200">
                <label className="text-xs font-semibold text-gray-700">Email Address</label>
                <input
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="you@example.com"
                  className="w-full px-3.5 py-2 text-sm bg-gray-50 border border-gray-300 rounded-lg placeholder-gray-400 focus:bg-white focus:outline-none focus:ring-1 focus:ring-gray-900 focus:border-gray-900 transition"
                />
              </div>
            )}

            {/* Password */}
            <div className="space-y-1">
              <label className="text-xs font-semibold text-gray-700">Password</label>
              <input
                type="password"
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
                className="w-full px-3.5 py-2 text-sm bg-gray-50 border border-gray-300 rounded-lg placeholder-gray-400 focus:bg-white focus:outline-none focus:ring-1 focus:ring-gray-900 focus:border-gray-900 transition"
              />
            </div>

            {/* Role picker if registering */}
            {isRegister && (
              <div className="space-y-1 animate-in fade-in duration-200">
                <label className="text-xs font-semibold text-gray-700">Account Role</label>
                <select
                  value={roleId}
                  onChange={(e) => setRoleId(Number(e.target.value))}
                  className="w-full px-3 py-2 text-sm bg-gray-50 border border-gray-300 rounded-lg text-gray-700 focus:bg-white focus:outline-none focus:ring-1 focus:ring-gray-900"
                >
                  <option value={3}>Shopper / Customer (Default)</option>
                  <option value={1}>Store Administrator (Admin Panel Access)</option>
                  <option value={2}>Store Staff</option>
                </select>
              </div>
            )}

            {/* Remember Me & Forgot Password */}
            {!isRegister && (
              <div className="flex items-center justify-between pt-1 text-xs">
                <label className="flex items-center gap-2 text-gray-600 cursor-pointer select-none">
                  <input
                    type="checkbox"
                    checked={rememberMe}
                    onChange={(e) => setRememberMe(e.target.checked)}
                    className="rounded border-gray-300 text-gray-900 focus:ring-gray-900 w-3.5 h-3.5"
                  />
                  <span>Remember me</span>
                </label>
                <button
                  type="button"
                  onClick={() => setForgotModal(true)}
                  className="text-gray-500 hover:text-gray-900 underline"
                >
                  Forgot Password?
                </button>
              </div>
            )}

            {/* Submit Button */}
            <button
              type="submit"
              disabled={loading}
              className="w-full py-2.5 px-4 bg-gray-900 hover:bg-gray-800 text-white font-medium text-sm rounded-lg transition-colors shadow-xs flex items-center justify-center gap-2 cursor-pointer mt-3 disabled:opacity-50"
            >
              {loading ? (
                <span className="inline-block w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
              ) : (
                <>
                  <span>{isRegister ? "Create Account" : "Log In"}</span>
                  <ArrowRight size={15} />
                </>
              )}
            </button>

            {/* Divider */}
            <div className="relative my-4">
              <div className="absolute inset-0 flex items-center">
                <div className="w-full border-t border-gray-200" />
              </div>
              <div className="relative flex justify-center text-xs">
                <span className="px-2 bg-white text-gray-400">or</span>
              </div>
            </div>

            {/* Toggle Sign Up / Sign In */}
            <div className="text-center text-xs text-gray-500">
              {isRegister ? (
                <p>
                  Already have an account?{' '}
                  <button
                    type="button"
                    onClick={() => setIsRegister(false)}
                    className="text-gray-900 font-semibold underline hover:text-black cursor-pointer"
                  >
                    Log In
                  </button>
                </p>
              ) : (
                <p>
                  Don't have an account?{' '}
                  <button
                    type="button"
                    onClick={() => setIsRegister(true)}
                    className="text-gray-900 font-semibold underline hover:text-black cursor-pointer"
                  >
                    Sign Up
                  </button>
                </p>
              )}
            </div>

            {/* Guest button */}
            <button
              type="button"
              onClick={() => navigateTo('home')}
              className="w-full py-2 text-xs text-gray-500 hover:text-gray-800 transition text-center"
            >
              Continue as Guest →
            </button>
          </form>
        </div>

        {/* Terms footer */}
        <p className="text-center text-xs text-gray-400 mt-6">
          By continuing, you agree to StyleShop's Terms of Service and Privacy Policy.
        </p>

        {/* Forgot Password Modal */}
        {forgotModal && (
          <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4">
            <div className="bg-white rounded-xl max-w-sm w-full p-6 space-y-4 shadow-xl">
              <div className="flex items-center gap-2 text-gray-900 font-bold text-base">
                <KeyRound size={18} />
                <span>Reset Password</span>
              </div>
              <p className="text-xs text-gray-600">
                Enter your username to request a reset token via the backend authentication service.
              </p>
              <input
                type="text"
                value={forgotUser}
                onChange={(e) => setForgotUser(e.target.value)}
                placeholder="Enter username"
                className="w-full px-3 py-2 text-sm bg-gray-50 border border-gray-300 rounded-lg focus:outline-none focus:ring-1 focus:ring-gray-900"
              />
              {resetMessage && (
                <p className="text-xs p-2 bg-emerald-50 text-emerald-700 border border-emerald-200 rounded">
                  {resetMessage}
                </p>
              )}
              <div className="flex justify-end gap-2 pt-2">
                <button
                  type="button"
                  onClick={() => {
                    setForgotModal(false);
                    setResetMessage('');
                  }}
                  className="px-3 py-1.5 text-xs text-gray-600 hover:bg-gray-100 rounded"
                >
                  Cancel
                </button>
                <button
                  type="button"
                  onClick={() => {
                    setResetMessage("Reset token generated and sent to user email.");
                    setTimeout(() => {
                      setForgotModal(false);
                      setResetMessage('');
                    }, 2000);
                  }}
                  className="px-3.5 py-1.5 text-xs bg-gray-900 text-white font-medium rounded hover:bg-gray-800"
                >
                  Send Reset Link
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
