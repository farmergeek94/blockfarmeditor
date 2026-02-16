export const UrlHelper = {
    /**
     * Get the base URL of the current page.
     * @returns {string} The base URL of the current page.
     */
    getBaseUrl() {
        const baseUrl = window.location.href.split('/umbraco')[0];
        return baseUrl;
    },
};