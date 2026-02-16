import { defineConfig } from "vite";

export default defineConfig({
    build: {
        lib: {
            entry: "src/front-end-elements.ts", // your web component source file
            formats: ["es"],
        },
        outDir: "../wwwroot/block-editor/dist", // all compiled files will be placed here
        emptyOutDir: true,
        sourcemap: true,
        rollupOptions: {
            external: [/^@umbraco/], // ignore the Umbraco Backoffice package in the build
        },
    },
    base: "/BlockFarmEditor.ClientScripts.RCL/block-editor/", // the base path of the app in the browser (used for assets)
});