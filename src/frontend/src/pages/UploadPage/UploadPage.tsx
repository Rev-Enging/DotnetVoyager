import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { UploadSimpleIcon, SpinnerIcon } from '@phosphor-icons/react';
import { analysisService } from '../../services/analysisService';
import { MainLayout } from '../../components/MainLayout/MainLayout';
import './UploadPage.scss';

export const UploadPage = () => {
    const [isDragging, setIsDragging] = useState(false);
    const [isUploading, setIsUploading] = useState(false);
    const navigate = useNavigate();

    const handleUpload = async (file: File) => {
        setIsUploading(true);
        try {
            const result = await analysisService.uploadAssembly(file);
            localStorage.setItem('currentAnalysisId', result.data.analysisId);
            navigate(`/analysis/${result.data.analysisId}`);
        } catch (error) {
            console.error("Upload failed", error);
            alert("Upload failed. Please check the file and try again.");
        } finally {
            setIsUploading(false);
        }
    };

    const onDrop = (e: React.DragEvent) => {
        e.preventDefault();
        setIsDragging(false);
        if (e.dataTransfer.files && e.dataTransfer.files[0]) {
            handleUpload(e.dataTransfer.files[0]);
        }
    };

    const onChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        if (e.target.files && e.target.files[0]) {
            handleUpload(e.target.files[0]);
        }
    };

    return (
        <MainLayout>
            <div className="upload-page">
                <div
                    className={`drop-zone ${isDragging ? 'dragging' : ''}`}
                    onDragOver={(e) => { e.preventDefault(); setIsDragging(true); }}
                    onDragLeave={() => setIsDragging(false)}
                    onDrop={onDrop}
                >
                    {isUploading ? (
                        <div className="loading-content">
                            <SpinnerIcon className="spin-icon" size={48} />
                            <p>Analyzing Assembly...</p>
                        </div>
                    ) : (
                        <>
                            <UploadSimpleIcon size={64} className="upload-icon" />
                            <h2>Upload .NET Assembly</h2>
                            <p>Drag & drop .exe or .dll files here</p>
                            <label className="browse-btn">
                                Browse Files
                                <input type="file" onChange={onChange} accept=".dll,.exe" hidden />
                            </label>
                        </>
                    )}
                </div>
            </div>
        </MainLayout>
    );
};