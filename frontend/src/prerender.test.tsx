import { describe, it, expect } from 'vitest';
import { render } from './prerender';
// The neutral bootstrap template, read as raw text (no node:fs needed in the app tsconfig).
import neutralHtml from '../index.html?raw';

// AC-11: the prerender must emit the hero copy, the section text, and the SEO head,
// so a crawler that runs no JavaScript still receives real content.
describe('landing prerender (AC-11)', () => {
  const { html, head } = render('https://quiztin.example');

  it('renders the hero copy and every section into static markup', () => {
    expect(html).toContain('Learning that');
    expect(html).toContain('Three steps, start to finish');
    expect(html).toContain('Feedback that sounds like a person');
    expect(html).toContain('Good questions, honest answers');
    expect(html).toContain('Ready to make quizzes feel calmer');
  });

  it('renders the default students hero, not an empty shell', () => {
    expect(html).toContain('Join your class');
    expect(html.length).toBeGreaterThan(5000);
  });

  it('carries the SEO head tags', () => {
    expect(head).toContain('<title>');
    expect(head).toContain('property="og:title"');
    expect(head).toContain('rel="canonical"');
    expect(head).toContain('application/ld+json');
  });
});

// AC-14: the neutral bootstrap that every non "/" route receives must stay neutral.
// It carries a generic title and no landing markup or metadata.
describe('neutral bootstrap document (AC-14)', () => {
  it('has a generic title, not the landing SEO title', () => {
    expect(neutralHtml).toContain('<title>Quiztin</title>');
  });

  it('contains no landing markup or Open Graph metadata', () => {
    expect(neutralHtml).not.toContain('Learning that');
    expect(neutralHtml).not.toContain('og:title');
    expect(neutralHtml).toContain('<div id="root"></div>');
  });
});
