import { useState } from 'react';
import classNames from 'classnames';
import type {AssemblyMetadataDto, AssemblyStatisticsDto, InheritanceGraphDto} from '../../models/analysis';
import { InheritanceTree } from './InheritanceTree';
import './InfoPanel.scss';

type InfoTab = 'inherit' | 'data';

interface InfoPanelProps {
    metadata?: AssemblyMetadataDto;
    statistics?: AssemblyStatisticsDto;
    graph?: InheritanceGraphDto;
    selectedToken?: number;
}

export const InfoPanel = ({ metadata, statistics, graph, selectedToken }: InfoPanelProps) => {
    const [activeTab, setActiveTab] = useState<InfoTab>('inherit');

    return (
        <aside className="info-panel">
            <div className="tabs-header">
                <div
                    className={classNames('tab', { active: activeTab === 'inherit' })}
                    onClick={() => setActiveTab('inherit')}
                >
                    Inheritance Tree
                </div>
                <div
                    className={classNames('tab', { active: activeTab === 'data' })}
                    onClick={() => setActiveTab('data')}
                >
                    Assembly Data
                </div>
            </div>

            <div className="panel-content">
                {activeTab === 'inherit' && (
                    <div className="inherit-view">
                         {graph ? (
                            <InheritanceTree graph={graph} selectedToken={selectedToken} />
                         ) : (
                            <div className="placeholder-text">Graph data not loaded.</div>
                         )}
                    </div>
                )}

                {activeTab === 'data' && metadata && statistics && (
                    <div className="data-view">
                        <div className="section">
                            <div className="section-title">General Info</div>
                            <div className="kv-item">
                                <label>Assembly Name</label>
                                <div className="value">{metadata.assemblyName}</div>
                            </div>
                            <div className="kv-grid">
                                <div className="kv-item">
                                    <label>Version</label>
                                    <div className="value value-blue">{metadata.version}</div>
                                </div>
                                <div className="kv-item">
                                    <label>Target</label>
                                    <div className="value value-purple">{metadata.targetFramework}</div>
                                </div>
                            </div>
                        </div>

                        <div className="section">
                            <div className="section-title">Structure Counts</div>
                            <div className="stats-grid">
                                <div className="stat-box">
                                    <label>Classes</label>
                                    <div className="stat-val">{statistics.classCount}</div>
                                </div>
                                <div className="stat-box">
                                    <label>Methods</label>
                                    <div className="stat-val">{statistics.methodCount}</div>
                                </div>
                                <div className="stat-box">
                                    <label>Interfaces</label>
                                    <div className="stat-val">{statistics.interfaceCount}</div>
                                </div>
                            </div>
                        </div>
                    </div>
                )}
            </div>
        </aside>
    );
};