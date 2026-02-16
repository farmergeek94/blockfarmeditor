import { defineConfig } from "vite";

export default defineConfig({
    build: {
        lib: {
            entry: "src/settings-dashboard.ts", // your web component source file
            formats: ["es"],
        },
        outDir: "../wwwroot/settings-dashboard/dist", // all compiled files will be placed here
        emptyOutDir: true,
        sourcemap: true,
        rollupOptions: {
            external: [/^@umbraco/], // ignore the Umbraco Backoffice package in the build
        },
    },
    base: "/App_Plugins/BlockFarmEditor.ClientScripts.RCL/settings-dashboard/", // the base path of the app in the browser (used for assets)
});