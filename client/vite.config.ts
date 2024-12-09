// import path from "path"
// import react from "@vitejs/plugin-react"
// import { defineConfig } from "vite"

// export default defineConfig({
//   plugins: [react()],
//   resolve: {
//     alias: {
//       "@": path.resolve(__dirname, "./src"),
//     },
//   },
// })
import path from "path"
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  define: {
    "process.env": {
      NODE_ENV: "production",
    },
  },
  plugins: [react()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  // 👇 Insert these lines
  build: {
    lib: {
      entry: "./src/main.tsx",
      name: "signalr-client",
      fileName: (format) => `signalr-client.${format}.js`,
    },
    rollupOptions:{
              external:['./src/lib/utils.ts']
    },
    target: "esnext",
  },
});