import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Sin la cabecera X-Powered-By, igual que la API, que no anuncia su servidor.
  poweredByHeader: false,
};

export default nextConfig;
