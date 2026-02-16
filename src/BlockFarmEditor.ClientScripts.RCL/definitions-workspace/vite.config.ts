import { defineConfig } from "vite";

export default defineConfig({
    build: {
        lib: {
            entry: {
                "definitions-workspace": "src/definitions-workspace.ts", // your web component source file
                "element-condition": "src/element-condition.ts", // your web component source file
            },
            formats: ["es"],
        },
        outDir: "../wwwroot/definitions-workspace/dist", // all compiled files will be placed here
        emptyOutDir: true,
        sourcemap: true,
        rollupOptions: {
            external: [/^@umbraco/], // ignore the Umbraco Backoffice package in the build
        },
    },
    base: "/App_Plugins/BlockFarmEditor.ClientScripts.RCL/definitions-workspace/", // the base path of the app in the browser (used for assets)
});