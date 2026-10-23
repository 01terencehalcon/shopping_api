'use client';

import React, { Suspense } from 'react';
import { AppProvider } from '../../lib/context';
import { ScreenRenderer } from '../page';

export default function LoginPage() {
  return (
    <Suspense fallback={<div className="min-h-screen flex items-center justify-center text-xs text-gray-400">Loading Login...</div>}>
      <AppProvider initialScreen="login">
        <ScreenRenderer initialRegister={false} />
      </AppProvider>
    </Suspense>
  );
}
