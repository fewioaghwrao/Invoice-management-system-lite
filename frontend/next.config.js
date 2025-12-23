/** @type {import('next').NextConfig} */
const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL;

if (!API_BASE) {
  console.warn("[WARN] NEXT_PUBLIC_API_BASE_URL is not set");
}

const nextConfig = {
  reactCompiler: true,

  async rewrites() {
    return API_BASE
      ? [
          {
            source: "/api/:path*",
            destination: `${API_BASE}/api/:path*`,
          },
        ]
      : [];
  },
};

module.exports = nextConfig;


