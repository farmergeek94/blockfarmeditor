import { defineConfig } from "vite";

export default defineConfig({
    build: {
        lib: {
            entry: {
                "blockfarmeditor-editor": "src/blockfarmeditor-editor.ts", // your web component source file
                "blockfarmeditor-modal": "src/components/blockfarmeditor-modal.ts", // block farm editor modal component
                "blockfarmeditor-add-block": "src/components/blockfarmeditor-add-block.ts", // add block modal component
                "blockfarmeditor-properties": "src/components/blockfarmeditor-properties.ts", // block properties modal component
                "blockfarmeditor-save-layout": "src/components/blockfarmeditor-save-layout.ts", // save layout modal component
                "blockfarmeditor-use-layout": "src/components/blockfarmeditor-use-layout.ts", // use layout modal component
            },
            formats: ["es"],
        },
        outDir: "../wwwroot/property-editor/dist/", // all compiled files will be placed here
        emptyOutDir: true,
        sourcemap: true,
        rollupOptions: {
            external: [/^@umbraco/], // ignore the Umbraco Backoffice package in the build
        },
    },
    base: "/App_Plugins/BlockFarmEditor.ClientScripts.RCL/property-editor/dist/", // the base path of the app in the browser (used for assets)
});