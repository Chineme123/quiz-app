import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { RouterProvider } from 'react-router';
import { Providers } from './app/Providers';
import { router } from './router';
import './styles/tailwind.css';

const root = document.getElementById('root');
if (!root) throw new Error('Root element #root not found.');

createRoot(root).render(
  <StrictMode>
    <Providers>
      <RouterProvider router={router} />
    </Providers>
  </StrictMode>,
);
