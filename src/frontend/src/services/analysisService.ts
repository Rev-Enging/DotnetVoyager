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

    downloadZip: async (id: string) => {
        return await axiosInstance.get(API_ROUTES.ANALYSIS.DOWNLOAD_ZIP(id), { responseType: 'blob' });
    }
};