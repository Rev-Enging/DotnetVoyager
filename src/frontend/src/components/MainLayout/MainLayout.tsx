import type { ReactNode } from 'react';
import { CubeIcon, FileZipIcon, CircleNotch, UploadSimpleIcon } from '@phosphor-icons/react';
import { useNavigate, Link, useLocation } from 'react-router-dom';
import './MainLayout.scss';

interface MainLayoutProps {
    children: ReactNode;
    onExportZip?: () => void;
    isExportingZip?: boolean;
    showUploadNew?: boolean;
}

export const MainLayout = ({ children, onExportZip, isExportingZip = false, showUploadNew = false }: MainLayoutProps) => {
    const navigate = useNavigate();
    const location = useLocation();

    const handleUploadNew = () => {
        navigate('/');
    };

    // Check if we are on the About page
    const isAboutPage = location.pathname === '/about';

    return (
        <div className="main-layout">
            <header className="main-header">
                <div className="brand">
                    <div className="logo-box">
                        <CubeIcon size={20} weight="fill" color="white" />
                    </div>
                    <span className="brand-text">Dotnet<span className="highlight">Voyager</span></span>
                </div>
                <nav className="nav-menu">
                    {!isAboutPage && <Link to="/about">About</Link>}
                    {showUploadNew && (
                        <button className="upload-btn" onClick={handleUploadNew}>
                            <UploadSimpleIcon size={18} />
                            Upload New
                        </button>
                    )}
                    {onExportZip && (
                        <button
                            className="export-btn"
                            onClick={onExportZip}
                            disabled={isExportingZip}
                        >
                            {isExportingZip ? (
                                <>
                                    <CircleNotch size={18} className="spinning" />
                                    Preparing...
                                </>
                            ) : (
                                <>
                                    <FileZipIcon size={18} />
                                    Export ZIP
                                </>
                            )}
                        </button>
                    )}
                </nav>
            </header>
            <div className="main-content">
                {children}
            </div>
        </div>
    );
};