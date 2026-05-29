import type {ReactNode} from 'react';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import Heading from '@theme/Heading';

import styles from './index.module.css';

export default function Home(): ReactNode {
  const {siteConfig} = useDocusaurusContext();
  return (
    <Layout
      title={`${siteConfig.title}`}
      description="OrderHub — Central order management API for e-commerce.">
      <header className={styles.heroBanner}>
        <img
          className={styles.heroLogo}
          src="/img/bouncy-ordering-groceries-completed.svg"
          alt="OrderHub"
        />
        <Heading as="h1">
          OrderHub
        </Heading>
        <p>
          Comprehensive architecture documentation for OrderHub.
        </p>
        <div className={styles.buttons}>
          <Link className={styles.primaryButton} to="/docs/getting-started/quick-start">
            Get Started
          </Link>
          <Link className={styles.outlineButton} to="/docs/architecture/introduction-goals">
            Architecture
          </Link>
        </div>
      </header>
    </Layout>
  );
}
