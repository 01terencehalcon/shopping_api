'use client';

import React, { Suspense } from 'react';
import { AppProvider } from '../../lib/context';
import { ScreenRenderer } from '../page';

export default function RegisterPage() {
  return (
    <Suspense fallback={<div className="min-h-screen flex items-center justify-center text-xs text-gray-400">Loading Register...</div>}>
      <AppProvider initialScreen="register">
        <ScreenRenderer initialRegister={true} />
      </AppProvider>
    </Suspense>
  );
}
