import { Outlet } from 'react-router';
import { Header } from './Header';

/** The frame for signed-in routes: header on top, routed content below. */
export function AppShell() {
  return (
    <div className="min-h-screen bg-bg">
      <Header />
      <main className="mx-auto w-full max-w-content px-6 py-9">
        <Outlet />
      </main>
    </div>
  );
}
