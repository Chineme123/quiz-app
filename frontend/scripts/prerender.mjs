// Postbuild prerender (spec 0003, AC-11 and AC-14). Runs after the client build
// and the SSR build. It renders the landing route to static HTML and writes it as a
// SEPARATE file (index.prerender.html), leaving the neutral bootstrap (index.html)
// untouched so the gateway can serve the landing markup only at exact "/".
import { readFileSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const dir = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(dir, '..'); // frontend/
const dist = path.join(root, 'dist');

// The canonical origin for SEO tags. Overridable at build time; see the spec follow up.
const siteUrl = process.env.SITE_URL ?? 'https://quiztin.up.railway.app';

const { render } = await import(path.join(root, 'dist-ssr', 'prerender.js'));
const { html, head } = render(siteUrl);

const template = readFileSync(path.join(dist, 'index.html'), 'utf8');

if (!template.includes('<div id="root"></div>')) {
  throw new Error('prerender: could not find <div id="root"></div> in dist/index.html');
}
if (!template.includes('<title>Quiztin</title>')) {
  throw new Error('prerender: could not find the placeholder <title>Quiztin</title> in dist/index.html');
}

// Inject the prerendered markup into #root, and swap the neutral <title> for the
// full landing head (its own <title>, description, Open Graph, Twitter, canonical,
// JSON-LD). The neutral index.html keeps its generic title, so no other route
// inherits this metadata.
let out = template.replace('<div id="root"></div>', `<div id="root">${html}</div>`);
out = out.replace('<title>Quiztin</title>', head);

writeFileSync(path.join(dist, 'index.prerender.html'), out);
console.log(`prerender: wrote dist/index.prerender.html (${out.length} bytes, body ${html.length} bytes)`);
process.exit(0);
