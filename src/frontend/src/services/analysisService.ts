import { axiosInstance } from '../api/axios';
import { API_ROUTES } from '../const/api-routes';

export const analysisService = {
    uploadAssembly: async (file: File) => {
        const formData = new FormData();
        formData.append('file', file);

        return await axiosInstance.post(API_ROUTES.ANALYSIS.UPLOAD, formData, {
            headers: {
                'Content-Type': 'multipart/form-data',
            },
        });
    },

    getStatus: async (analysisId: string) => {
        return await axiosInstance.get(API_ROUTES.ANALYSIS.GET_STATUS(analysisId));
    },

    getAssemblyTree: async (analysisId: string) => {
        return await axiosInstance.get(API_ROUTES.ANALYSIS.GET_TREE(analysisId));
    }
};