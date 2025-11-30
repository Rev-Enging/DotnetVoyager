import { useEffect, useState } from 'react';
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

        if (type === 'Method' || type === 'Class') {
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
            // Шаг 1: Инициируем генерацию ZIP
            await analysisService.prepareZip(analysisId);

            // Шаг 2: Ждём, пока ZIP будет готов (можно добавить polling статуса)
            // Для простоты подождём 2 секунды
            await new Promise(resolve => setTimeout(resolve, 2000));

            // Шаг 3: Скачиваем готовый ZIP
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
                {/* Floating toggle button когда сайдбар свернут */}
                {isSidebarCollapsed && (
                    <button className="floating-toggle-btn" onClick={toggleSidebar}>
                        <ArrowsOutLineHorizontalIcon weight="bold" />
                    </button>
                )}

                <aside className={classNames('left-sidebar', { collapsed: isSidebarCollapsed })}>
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