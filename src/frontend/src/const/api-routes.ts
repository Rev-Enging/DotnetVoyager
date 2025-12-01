const BASE_URL = import.meta.env.VITE_BACKEND_URL;

export const API_ROUTES = {
    BASE: BASE_URL,
    ANALYSIS: {
        // POST api/Analysis/upload
        UPLOAD: 'Analysis/upload',

        // GET api/Analysis/{id}/status
        GET_STATUS: (id: string) => `Analysis/${id}/status`,

        // POST api/Analysis/{id}/retry
        RETRY: (id: string) => `Analysis/${id}/retry`,

        // GET api/Analysis/{id}/metadata
        GET_METADATA: (id: string) => `Analysis/${id}/metadata`,

        // GET api/Analysis/{id}/statistics
        GET_STATISTICS: (id: string) => `Analysis/${id}/statistics`,

        // GET api/Analysis/{id}/dependencies
        GET_DEPENDENCIES: (id: string) => `Analysis/${id}/dependencies`,

        // GET api/Analysis/{id}/assembly-tree
        GET_TREE: (id: string) => `Analysis/${id}/assembly-tree`,

        // GET api/Analysis/{id}/inheritance-graph
        GET_GRAPH: (id: string) => `Analysis/${id}/inheritance-graph`,

        // GET api/Analysis/{id}/decompile/{token}
        GET_DECOMPILED_CODE: (id: string, token: number) => `Analysis/${id}/decompile/${token}`,

        // POST api/Analysis/{id}/prepare-zip
        PREPARE_ZIP: (id: string) => `Analysis/${id}/prepare-zip`,

        // GET api/Analysis/{id}/download-zip
        DOWNLOAD_ZIP: (id: string) => `Analysis/${id}/download-zip`,
    }
};