'use client';

import React, { Suspense } from 'react';
import { AppProvider } from '../../lib/context';
import { ScreenRenderer } from '../page';

export default function AdminPage() {
  return (
    <Suspense fallback={<div className="min-h-screen flex items-center justify-center text-xs text-gray-400">Loading Admin Dashboard...</div>}>
      <AppProvider initialScreen="admin">
        <ScreenRenderer />
      </AppProvider>
    </Suspense>
  );
}
