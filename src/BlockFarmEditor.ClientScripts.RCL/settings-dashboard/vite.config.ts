import { defineConfig } from "vite";

export default defineConfig({
    build: {
        lib: {
            entry: {
                "settings-dashboard": "src/settings-dashboard.ts", // your web component source file
                "bf-export-modal": "src/components/bf-export-modal.ts", // export modal component
                "bf-import-modal": "src/components/bf-import-modal.ts", // import modal component
            },
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