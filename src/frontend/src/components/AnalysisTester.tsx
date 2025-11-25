// src/components/AnalysisTester.tsx
import { useState } from 'react';
import { analysisService } from '../services/analysisService';

export const AnalysisTester = () => {
    const [file, setFile] = useState<File | null>(null);
    const [analysisId, setAnalysisId] = useState<string>('');
    const [statusData, setStatusData] = useState<any>(null);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string>('');

    // 1. Обробка вибору файлу
    const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        if (e.target.files && e.target.files.length > 0) {
            setFile(e.target.files[0]);
            setError('');
        }
    };

    // 2. Відправка на сервер
    const handleUpload = async () => {
        if (!file) return;

        setIsLoading(true);
        setError('');

        try {
            const response = await analysisService.uploadAssembly(file);
            // Бекенд повертає { analysisId: "..." }
            setAnalysisId(response.data.analysisId);
            console.log("Uploaded! ID:", response.data.analysisId);
        } catch (err: any) {
            console.error(err);
            setError('Upload failed: ' + (err.response?.data?.detail || err.message));
        } finally {
            setIsLoading(false);
        }
    };

    // 3. Перевірка статусу
    const handleCheckStatus = async () => {
        if (!analysisId) return;

        try {
            const response = await analysisService.getStatus(analysisId);
            setStatusData(response.data);
        } catch (err: any) {
            setError('Status check failed');
        }
    };

    return (
        <div className="p-6 max-w-md mx-auto bg-[#252526] rounded-lg border border-[#333] shadow-xl text-gray-300 mt-10">
            <h2 className="text-xl font-bold text-white mb-4">Analysis Tester</h2>

            {/* FILE INPUT */}
            <div className="mb-4">
                <input
                    type="file"
                    onChange={handleFileChange}
                    className="block w-full text-sm text-gray-400
                        file:mr-4 file:py-2 file:px-4
                        file:rounded file:border-0
                        file:text-sm file:font-semibold
                        file:bg-purple-600 file:text-white
                        hover:file:bg-purple-700
                        cursor-pointer"
                />
            </div>

            {/* UPLOAD BUTTON */}
            <button
                onClick={handleUpload}
                disabled={!file || isLoading}
                className={`w-full py-2 px-4 rounded font-bold mb-4 transition-colors ${
                    !file || isLoading 
                    ? 'bg-gray-600 cursor-not-allowed' 
                    : 'bg-green-600 hover:bg-green-500 text-white'
                }`}
            >
                {isLoading ? 'Uploading...' : 'Upload Assembly'}
            </button>

            {/* ERROR MESSAGE */}
            {error && (
                <div className="p-3 mb-4 bg-red-900/50 border border-red-700 text-red-200 text-sm rounded">
                    {error}
                </div>
            )}

            {/* RESULTS AREA (Show only if we have an ID) */}
            {analysisId && (
                <div className="mt-6 border-t border-[#444] pt-4">
                    <p className="text-xs text-gray-500 mb-1">Analysis ID:</p>
                    <div className="font-mono text-sm bg-[#111] p-2 rounded text-purple-400 mb-4 break-all border border-[#333]">
                        {analysisId}
                    </div>

                    <button
                        onClick={handleCheckStatus}
                        className="w-full py-2 px-4 bg-blue-600 hover:bg-blue-500 text-white rounded font-bold mb-4"
                    >
                        Check Status
                    </button>

                    {/* STATUS DISPLAY */}
                    {statusData && (
                        <div className="bg-[#1e1e1e] p-3 rounded border border-[#333] text-sm font-mono">
                            <div className="flex justify-between mb-2">
                                <span className="text-gray-500">Overall:</span>
                                <span className={statusData.overallStatus === 'Completed' ? 'text-green-400' : 'text-yellow-400'}>
                                    {statusData.overallStatus}
                                </span>
                            </div>

                            <div className="text-xs text-gray-500 mb-1">Steps:</div>
                            <ul className="space-y-1">
                                {statusData.steps?.map((step: any, idx: number) => (
                                    <li key={idx} className="flex justify-between">
                                        <span>{step.stepName}</span>
                                        <span className={
                                            step.status === 'Completed' ? 'text-green-500' :
                                            step.status === 'Failed' ? 'text-red-500' : 'text-yellow-500'
                                        }>
                                            {step.status}
                                        </span>
                                    </li>
                                ))}
                            </ul>
                        </div>
                    )}
                </div>
            )}
        </div>
    );
};