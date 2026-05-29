import type {SidebarsConfig} from '@docusaurus/plugin-content-docs';

const sidebars: SidebarsConfig = {
  docsSidebar: [
    'intro',
    {
      type: 'category',
      label: 'Getting Started',
      collapsed: false,
      items: [
        'getting-started/quick-start',
        'getting-started/local-development',
        'getting-started/running-tests',
        'getting-started/seed-data',
      ],
    },
    {
      type: 'category',
      label: 'Architecture (arc42)',
      collapsed: false,
      items: [
        'architecture/introduction-goals',
        'architecture/constraints',
        'architecture/context',
        'architecture/solution-strategy',
        'architecture/building-blocks',
        'architecture/runtime-view',
        'architecture/deployment-view',
        'architecture/crosscutting',
        {
          type: 'category',
          label: '9. Architecture Decisions',
          collapsed: false,
          items: [
            'architecture/decisions/index',
            'architecture/decisions/adr-001-postgresql',
            'architecture/decisions/adr-002-pessimistic-locking',
            'architecture/decisions/adr-003-mapster',
            'architecture/decisions/adr-004-memory-cache',
            'architecture/decisions/adr-005-password-hasher',
            'architecture/decisions/adr-006-repository-unit-of-work',
            'architecture/decisions/adr-007-serilog-seq',
            'architecture/decisions/adr-008-html-sanitizer',
            'architecture/decisions/adr-009-result-pattern',
            'architecture/decisions/adr-010-category-string',
            'architecture/decisions/adr-011-clean-architecture',
          ],
        },
        'architecture/quality-requirements',
        'architecture/risks-technical-debt',
        'architecture/glossary',
      ],
    },
    {
      type: 'category',
      label: 'API Reference',
      items: [
        'api-reference/overview',
        'api-reference/authentication',
        'api-reference/products',
        'api-reference/orders',
        'api-reference/admin-reports',
        'api-reference/health-checks',
      ],
    },
    {
      type: 'category',
      label: 'Guides',
      items: [
        'guides/deployment',
        'guides/observability',
        'guides/caching',
      ],
    },
  ],
};

export default sidebars;
