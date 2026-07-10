import { useEffect, useRef, useState } from 'react';
import { Link, useNavigate } from 'react-router';
import { User, SignOut } from '@phosphor-icons/react';
import { useAuth } from '@/lib/auth/useAuth';
import { Icon } from '@/components/ui';
import './ProfileMenu.css';

/** Avatar button that discloses a small menu: manage profile, sign out. */
export function ProfileMenu() {
  const { user, signOut } = useAuth();
  const [open, setOpen] = useState(false);
  const navigate = useNavigate();
  const containerRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    if (!open) return;
    const onPointerDown = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) setOpen(false);
    };
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        setOpen(false);
        triggerRef.current?.focus();
      }
    };
    document.addEventListener('mousedown', onPointerDown);
    document.addEventListener('keydown', onKeyDown);
    return () => {
      document.removeEventListener('mousedown', onPointerDown);
      document.removeEventListener('keydown', onKeyDown);
    };
  }, [open]);

  const initial = (user?.email ?? user?.role ?? '?').charAt(0).toUpperCase();

  const handleSignOut = () => {
    setOpen(false);
    void signOut().then(() => navigate('/sign-in'));
  };

  return (
    <div className="qz-profilemenu" ref={containerRef}>
      <button
        ref={triggerRef}
        type="button"
        className="qz-profilemenu__trigger"
        aria-haspopup="menu"
        aria-expanded={open}
        onClick={() => setOpen((o) => !o)}
      >
        <span className="qz-profilemenu__avatar" aria-hidden="true">
          {initial}
        </span>
        <span className="qz-profilemenu__role">{user?.role ?? 'Account'}</span>
      </button>

      {open && (
        <div className="qz-profilemenu__menu" role="menu">
          <Link
            to="/profile"
            role="menuitem"
            className="qz-profilemenu__item"
            onClick={() => setOpen(false)}
          >
            <Icon icon={User} />
            <span>Manage profile</span>
          </Link>
          <button type="button" role="menuitem" className="qz-profilemenu__item" onClick={handleSignOut}>
            <Icon icon={SignOut} />
            <span>Sign out</span>
          </button>
        </div>
      )}
    </div>
  );
}
