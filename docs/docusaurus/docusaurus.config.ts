import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'OrderHub Documentation',
  tagline: 'Central Order Management API for E-Commerce',
  favicon: 'img/bouncy-ordering-groceries-completed.svg',

  future: {
    v4: true,
  },

  url: 'https://orderhub.dev',
  baseUrl: '/',

  organizationName: 'FTI',
  projectName: 'OrderHub',

  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',

  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
        },
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  themes: ['@docusaurus/theme-mermaid'],
  markdown: {
    mermaid: true,
  },

  themeConfig: {
    image: 'img/docusaurus-social-card.jpg',
    colorMode: {
      defaultMode: 'light',
      disableSwitch: false,
      respectPrefersColorScheme: true,
    },
    navbar: {
      title: 'OrderHub',
      logo: {
        alt: 'OrderHub Logo',
        src: 'img/bouncy-ordering-groceries-completed.svg',
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'docsSidebar',
          position: 'left',
          label: 'Docs',
        },
        {
          to: '/docs/architecture/introduction-goals',
          label: 'Architecture',
          position: 'left',
        },
        {
          to: '/docs/api-reference/overview',
          label: 'API',
          position: 'left',
        },
        {
          href: 'https://github.com/datngw/OrderHub',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Docs',
          items: [
            {
              label: 'Getting Started',
              to: '/docs/getting-started/quick-start',
            },
            {
              label: 'Architecture',
              to: '/docs/architecture/introduction-goals',
            },
            {
              label: 'API Reference',
              to: '/docs/api-reference/overview',
            },
          ],
        },
        {
          title: 'More',
          items: [
            {
              label: 'GitHub',
              href: 'https://github.com/datngw/OrderHub',
            },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} Dat Nguyen Van (FTI). Built with Docusaurus.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ['csharp', 'json', 'bash', 'powershell'],
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
