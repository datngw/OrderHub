import type {ReactNode} from 'react';
import clsx from 'clsx';
import Heading from '@theme/Heading';
import styles from './styles.module.css';

type FeatureItem = {
  title: string;
  icon: string;
  description: ReactNode;
};

const FeatureList: FeatureItem[] = [
  {
    title: 'Clean Architecture',
    icon: '🏗️',
    description: (
      <>
        Strict 4-layer separation — <strong>Domain → Application → Infrastructure → Api</strong> —
        with dependency inversion ensuring business logic stays independent of frameworks and infrastructure.
      </>
    ),
  },
  {
    title: 'CQRS via MediatR',
    icon: '⚡',
    description: (
      <>
        Separate commands and queries with MediatR pipeline behaviors for validation, logging,
        and performance tracking. All business logic lives in handlers, never in endpoints.
      </>
    ),
  },
  {
    title: 'Production Observability',
    icon: '📊',
    description: (
      <>
        Structured logging via Serilog to Console, rolling JSON files, and Seq.
        Sensitive data redaction, enrichers for environment/process/thread/span correlation.
        OpenTelemetry ready.
      </>
    ),
  },
  {
    title: 'Comprehensive Testing',
    icon: '✅',
    description: (
      <>
        52 unit tests covering all handlers and validators. Integration tests with
        Testcontainers (real PostgreSQL). Concurrency test verifies pessimistic locking under real load.
      </>
    ),
  },
  {
    title: 'Security Hardened',
    icon: '🛡️',
    description: (
      <>
        JWT auth with per-endpoint rate limiting, HTML input sanitization (XSS prevention),
        security headers (HSTS, CSP, X-Frame-Options), and password complexity rules.
      </>
    ),
  },
];

function Feature({title, icon, description}: FeatureItem) {
  return (
    <div className={styles.featureItem}>
      <div className={styles.featureIconWrap}>
        <span className={styles.featureIcon}>{icon}</span>
      </div>
      <div className={styles.featureBody}>
        <Heading as="h3">{title}</Heading>
        <p>{description}</p>
      </div>
    </div>
  );
}

export default function HomepageFeatures(): ReactNode {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className={styles.featureGrid}>
          {FeatureList.map((props, idx) => (
            <Feature key={idx} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
}
