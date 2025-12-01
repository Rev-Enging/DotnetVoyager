import { axiosInstance } from '../api/axios';
import { API_ROUTES } from '../const/api-routes';
import type {
    AnalysisStatusDto,
    AssemblyDependenciesDto,
    AssemblyMetadataDto,
    AssemblyStatisticsDto,
    AssemblyTreeDto,
    DecompiledCodeDto,
    InheritanceGraphDto,
    UploadAssemblyResultDto
} from '../models/analysis';

export const analysisService = {
    uploadAssembly: async (file: File) => {
        const formData = new FormData();
        formData.append('file', file);
        return await axiosInstance.post<UploadAssemblyResultDto>(API_ROUTES.ANALYSIS.UPLOAD, formData, {
            headers: { 'Content-Type': 'multipart/form-data' },
        });
    },

    getStatus: async (id: string) => {
        return await axiosInstance.get<AnalysisStatusDto>(API_ROUTES.ANALYSIS.GET_STATUS(id));
    },

    getMetadata: async (id: string) => {
        return await axiosInstance.get<AssemblyMetadataDto>(API_ROUTES.ANALYSIS.GET_METADATA(id));
    },

    getStatistics: async (id: string) => {
        return await axiosInstance.get<AssemblyStatisticsDto>(API_ROUTES.ANALYSIS.GET_STATISTICS(id));
    },

    getDependencies: async (id: string) => {
        return await axiosInstance.get<AssemblyDependenciesDto>(API_ROUTES.ANALYSIS.GET_DEPENDENCIES(id));
    },

    getAssemblyTree: async (id: string) => {
        return await axiosInstance.get<AssemblyTreeDto>(API_ROUTES.ANALYSIS.GET_TREE(id));
    },

    getGraph: async (id: string) => {
        return await axiosInstance.get<InheritanceGraphDto>(API_ROUTES.ANALYSIS.GET_GRAPH(id));
    },

    getDecompiledCode: async (id: string, token: number) => {
        return await axiosInstance.get<DecompiledCodeDto>(API_ROUTES.ANALYSIS.GET_DECOMPILED_CODE(id, token));
    },

    prepareZip: async (id: string) => {
        return await axiosInstance.post(API_ROUTES.ANALYSIS.PREPARE_ZIP(id));
    },

    downloadZip: async (id: string) => {
        const response = await axiosInstance.get(API_ROUTES.ANALYSIS.DOWNLOAD_ZIP(id), {
            responseType: 'blob'
        });

        // Create a link for downloading
        const url = window.URL.createObjectURL(new Blob([response.data]));
        const link = document.createElement('a');
        link.href = url;

        // Retrieve the file name from headers or use the default
        const contentDisposition = response.headers['content-disposition'];
        let fileName = `analysis_${id}.zip`;

        if (contentDisposition) {
            const fileNameMatch = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
            if (fileNameMatch && fileNameMatch[1]) {
                fileName = fileNameMatch[1].replace(/['"]/g, '');
            }
        }

        link.setAttribute('download', fileName);
        document.body.appendChild(link);
        link.click();
        link.remove();
        window.URL.revokeObjectURL(url);

        return response;
    }
};