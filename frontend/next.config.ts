import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Sin la cabecera X-Powered-By, igual que la API, que no anuncia su servidor.
  poweredByHeader: false,
  experimental: {
    // Los documentos de una solicitud pesan hasta 10 MB (RN-18) y se suben con una Server Action. Se deja margen para
    // el resto del formulario multipart. proxy.ts copia el cuerpo de las rutas /portal, así que su límite es el mismo.
    serverActions: { bodySizeLimit: "11mb" },
    proxyClientMaxBodySize: "11mb",
  },
};

export default nextConfig;
