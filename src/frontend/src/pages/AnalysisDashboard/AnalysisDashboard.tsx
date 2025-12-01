import { useEffect, useState, useRef, useCallback } from 'react';
import { useParams } from 'react-router-dom';
import { MainLayout } from '../../components/MainLayout/MainLayout';
import { AssemblyTree } from '../../components/AssemblyTree/AssemblyTree';
import { CodeViewer } from '../../components/CodeViewer/CodeViewer';
import { InfoPanel } from '../../components/InfoPanel/InfoPanel';
import { analysisService } from '../../services/analysisService';
import type {
    AnalysisOverallStatus,
    AssemblyTreeNodeType,
    AssemblyTreeDto,
    AssemblyMetadataDto,
    AssemblyStatisticsDto,
    DecompiledCodeDto,
    InheritanceGraphDto
} from '../../models/analysis';
import { MagnifyingGlassIcon, ArrowsInLineHorizontalIcon, ArrowsOutLineHorizontalIcon } from '@phosphor-icons/react';
import classNames from 'classnames';
import './AnalysisDashboard.scss';

const MIN_SIDEBAR_WIDTH = 100;
const MAX_SIDEBAR_WIDTH = 600;
const DEFAULT_SIDEBAR_WIDTH = 288;

export const AnalysisDashboard = () => {
    const { id } = useParams<{ id: string }>();
    const analysisId = id!;

    // State
    const [status, setStatus] = useState<AnalysisOverallStatus>('Pending');
    const [graph, setGraph] = useState<InheritanceGraphDto | undefined>();
    const [tree, setTree] = useState<AssemblyTreeDto | null>(null);
    const [metadata, setMetadata] = useState<AssemblyMetadataDto | undefined>();
    const [stats, setStats] = useState<AssemblyStatisticsDto | undefined>();
    const [selectedToken, setSelectedToken] = useState<number | undefined>();
    const [codeData, setCodeData] = useState<DecompiledCodeDto | null>(null);
    const [viewMode, setViewMode] = useState<'csharp' | 'il' | 'both'>('csharp');
    const [isCodeLoading, setIsCodeLoading] = useState(false);
    const [searchQuery, setSearchQuery] = useState('');
    const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false);
    const [isExportingZip, setIsExportingZip] = useState(false);
    const [sidebarWidth, setSidebarWidth] = useState(DEFAULT_SIDEBAR_WIDTH);

    const isResizingRef = useRef(false);
    const startXRef = useRef(0);
    const startWidthRef = useRef(0);

    // Polling Status
    useEffect(() => {
        let interval: number;

        const checkStatus = async () => {
            try {
                const res = await analysisService.getStatus(analysisId);
                const currentStatus = res.data.overallStatus;
                setStatus(currentStatus);

                if (currentStatus === 'Completed') {
                    clearInterval(interval);
                    loadAnalysisData();
                } else if (currentStatus === 'Failed') {
                    clearInterval(interval);
                    alert("Analysis failed.");
                }
            } catch (err) {
                console.error(err);
            }
        };

        checkStatus();
        interval = setInterval(checkStatus, 2000);

        return () => clearInterval(interval);
    }, [analysisId]);

    const loadAnalysisData = async () => {
        try {
            const [treeRes, metaRes, statsRes, graphRes] = await Promise.all([
                analysisService.getAssemblyTree(analysisId),
                analysisService.getMetadata(analysisId),
                analysisService.getStatistics(analysisId),
                analysisService.getGraph(analysisId)
            ]);
            setTree(treeRes.data);
            setMetadata(metaRes.data);
            setStats(statsRes.data);
            setGraph(graphRes.data);
        } catch (e) {
            console.error("Failed loading data", e);
        }
    };

    const handleNodeSelect = async (token: number, type: AssemblyTreeNodeType) => {
        setSelectedToken(token);

        if (type === 'Method' || type === 'Class' || type === 'Interface' || type === 'Struct') {
            setIsCodeLoading(true);
            try {
                const res = await analysisService.getDecompiledCode(analysisId, token);
                setCodeData(res.data);
            } catch (e) {
                console.error("Decompilation failed", e);
                setCodeData(null);
            } finally {
                setIsCodeLoading(false);
            }
        }
    };

    const handleExportZip = async () => {
        if (isExportingZip) return;

        setIsExportingZip(true);
        try {
            await analysisService.prepareZip(analysisId);
            await new Promise(resolve => setTimeout(resolve, 2000));
            await analysisService.downloadZip(analysisId);
            console.log('ZIP downloaded successfully');
        } catch (e) {
            console.error('Export failed:', e);
            alert('Failed to export ZIP. Please try again.');
        } finally {
            setIsExportingZip(false);
        }
    };

    const toggleSidebar = () => {
        setIsSidebarCollapsed(!isSidebarCollapsed);
    };

    const handleMouseDown = useCallback((e: React.MouseEvent) => {
        e.preventDefault();
        isResizingRef.current = true;
        startXRef.current = e.clientX;
        startWidthRef.current = sidebarWidth;
        document.body.style.cursor = 'col-resize';
        document.body.style.userSelect = 'none';
    }, [sidebarWidth]);

    useEffect(() => {
        const handleMouseMove = (e: MouseEvent) => {
            if (!isResizingRef.current) return;

            const delta = e.clientX - startXRef.current;
            const newWidth = startWidthRef.current + delta;

            if (newWidth >= MIN_SIDEBAR_WIDTH && newWidth <= MAX_SIDEBAR_WIDTH) {
                setSidebarWidth(newWidth);
            }
        };

        const handleMouseUp = () => {
            if (isResizingRef.current) {
                isResizingRef.current = false;
                document.body.style.cursor = '';
                document.body.style.userSelect = '';
            }
        };

        document.addEventListener('mousemove', handleMouseMove);
        document.addEventListener('mouseup', handleMouseUp);

        return () => {
            document.removeEventListener('mousemove', handleMouseMove);
            document.removeEventListener('mouseup', handleMouseUp);
        };
    }, []);

    if (status !== 'Completed' && !tree) {
        return (
            <MainLayout showUploadNew={false}>
                <div className="loading-screen">Processing Assembly... ({status})</div>
            </MainLayout>
        );
    }

    return (
        <MainLayout onExportZip={handleExportZip} isExportingZip={isExportingZip} showUploadNew={true}>
            <div className="dashboard-container">
                {isSidebarCollapsed && (
                    <button className="floating-toggle-btn" onClick={toggleSidebar}>
                        <ArrowsOutLineHorizontalIcon weight="bold" />
                    </button>
                )}

                <aside
                    className={classNames('left-sidebar', { collapsed: isSidebarCollapsed })}
                    style={{ width: isSidebarCollapsed ? 0 : sidebarWidth }}
                >
                    <div className="sidebar-header">
                        <span className="title">Assembly Explorer</span>
                        <button className="icon-btn" onClick={toggleSidebar}>
                            <ArrowsInLineHorizontalIcon weight="bold" />
                        </button>
                    </div>
                    <div className="search-box">
                        <div className="input-wrapper">
                            <MagnifyingGlassIcon className="search-icon" />
                            <input
                                type="text"
                                placeholder="Search types..."
                                value={searchQuery}
                                onChange={(e) => setSearchQuery(e.target.value)}
                            />
                        </div>
                    </div>
                    <div className="tree-content">
                        {tree && (
                            <AssemblyTree
                                node={tree.root}
                                onSelectNode={handleNodeSelect}
                                selectedToken={selectedToken}
                                searchQuery={searchQuery}
                            />
                        )}
                    </div>
                </aside>

                {!isSidebarCollapsed && (
                    <div
                        className="sidebar-resizer"
                        onMouseDown={handleMouseDown}
                    />
                )}

                <main className="center-panel">
                    <CodeViewer
                        csharpCode={codeData?.cSharpCode}
                        ilCode={codeData?.ilCode}
                        viewMode={viewMode}
                        onChangeViewMode={setViewMode}
                        isLoading={isCodeLoading}
                    />
                </main>

                <InfoPanel
                    metadata={metadata}
                    statistics={stats}
                    graph={graph}
                    selectedToken={selectedToken}
                />
            </div>
        </MainLayout>
    );
};