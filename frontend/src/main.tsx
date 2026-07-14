import { createRoot, hydrateRoot } from 'react-dom/client';
import { AppRoot } from './AppRoot';
import { router } from './router';
import './styles/tailwind.css';

const root = document.getElementById('root');
if (!root) throw new Error('Root element #root not found.');

const tree = <AppRoot router={router} />;

// The landing page is prerendered at "/", so that document arrives with markup
// already inside #root: hydrate it, no flash, no repaint. Every other route ships
// the neutral bootstrap with an empty #root, so mount fresh (spec 0003, AC-11/AC-14).
if (root.firstElementChild) {
  hydrateRoot(root, tree);
} else {
  createRoot(root).render(tree);
}
