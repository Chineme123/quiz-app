import { describe, it, expect } from 'vitest';
import { buildHeadTags } from './seo';

describe('buildHeadTags (AC-11)', () => {
  const head = buildHeadTags('https://quiztin.example');

  it('emits a title and meta description', () => {
    expect(head).toMatch(/<title>[^<]*Quiztin[^<]*<\/title>/);
    expect(head).toContain('<meta name="description" content="');
  });

  it('emits a canonical link at the site root', () => {
    expect(head).toContain('<link rel="canonical" href="https://quiztin.example/" />');
  });

  it('emits Open Graph and Twitter card tags', () => {
    expect(head).toContain('<meta property="og:type" content="website" />');
    expect(head).toContain('<meta property="og:title" content="');
    expect(head).toContain('<meta property="og:url" content="https://quiztin.example/" />');
    expect(head).toContain('<meta property="og:image" content="https://quiztin.example/og-image.png" />');
    expect(head).toContain('<meta name="twitter:card" content="summary_large_image" />');
    expect(head).toContain('<meta name="twitter:image" content="https://quiztin.example/og-image.png" />');
  });

  it('normalises a trailing slash in the site url', () => {
    const withSlash = buildHeadTags('https://quiztin.example/');
    expect(withSlash).toContain('href="https://quiztin.example/"');
    expect(withSlash).not.toContain('quiztin.example//');
  });

  it('emits valid product JSON-LD with a free offer', () => {
    const match = head.match(/<script type="application\/ld\+json">(.*?)<\/script>/s);
    const json = match?.[1] ?? '';
    expect(json).not.toBe('');
    const data = JSON.parse(json.replace(/\\u003c/g, '<')) as {
      '@type': string;
      applicationCategory: string;
      offers: { price: string };
    };
    expect(data['@type']).toBe('SoftwareApplication');
    expect(data.applicationCategory).toBe('EducationalApplication');
    expect(data.offers.price).toBe('0');
  });

  it('escapes angle brackets so the JSON-LD cannot break out of its script tag', () => {
    // The serialized JSON must not contain a raw "</script" sequence.
    expect(head).not.toMatch(/<\/script[^>]*>\s*(?!$)/i);
    expect(head.split('</script>')).toHaveLength(2);
  });
});
