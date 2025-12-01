import { useMemo } from 'react';
import classNames from 'classnames';
import type {
    InheritanceGraphDto,
    InheritanceGraphNodeDto,
    InheritanceGraphNodeType
} from '../../models/analysis';
import './InheritanceTree.scss';

interface InheritanceTreeProps {
    graph: InheritanceGraphDto;
    selectedToken?: number;
}

// Внутрішня структура для рекурсивного відображення
interface TreeRenderNode {
    data: InheritanceGraphNodeDto;
    parents: TreeRenderNode[];
}

export const InheritanceTree = ({ graph, selectedToken }: InheritanceTreeProps) => {

    // 1. Будуємо дерево від вибраного елемента "вгору" (до батьків та інтерфейсів)
    const rootTree = useMemo(() => {
        if (!selectedToken || !graph) return null;

        // Знаходимо ноду, яку вибрав користувач
        // Note: TokenId у графі int, але порівнюємо обережно
        const rootNodeData = graph.nodes.find(n => n.tokenId === selectedToken);
        if (!rootNodeData) return null;

        // Рекурсивна функція для пошуку батьків (Target в edges)
        const buildTree = (nodeData: InheritanceGraphNodeDto, visited = new Set<string>()): TreeRenderNode => {
            if (visited.has(nodeData.id)) {
                return { data: nodeData, parents: [] }; // Prevent cycles
            }
            visited.add(nodeData.id);

            // Знаходимо ребра, де Source == поточному вузлу (тобто поточний успадковує Target)
            const parentEdges = graph.edges.filter(e => e.source === nodeData.id);

            const parents = parentEdges
                .map(edge => graph.nodes.find(n => n.id === edge.target))
                .filter((n): n is InheritanceGraphNodeDto => !!n)
                .map(n => buildTree(n, new Set(visited)));

            return {
                data: nodeData,
                parents: parents
            };
        };

        return buildTree(rootNodeData);
    }, [graph, selectedToken]);

    if (!selectedToken) {
        return <div className="placeholder-text">Select a type in the Assembly Explorer to view inheritance.</div>;
    }

    if (!rootTree) {
        return <div className="placeholder-text">No inheritance data found for this type.</div>;
    }

    return (
        <div className="inheritance-tree-container">
            <TreeNodeItem node={rootTree} isRoot={true} />
        </div>
    );
};

// --- Recursive Node Component ---

const TreeNodeItem = ({ node, isRoot = false }: { node: TreeRenderNode, isRoot?: boolean }) => {

    const getBadge = (type: InheritanceGraphNodeType) => {
        switch (type) {
            case 'Class': return { label: 'C', className: 'badge-class', title: 'Class' };
            case 'Interface': return { label: 'I', className: 'badge-interface', title: 'Interface' };
            case 'Struct': return { label: 'S', className: 'badge-struct', title: 'Struct' };
            case 'Enum': return { label: 'E', className: 'badge-enum', title: 'Enum' };
            default: return { label: '?', className: 'badge-unknown', title: 'Unknown' };
        }
    };

    const badge = getBadge(node.data.type);

    return (
        <div className="tree-branch">
            {/* Сама нода */}
            <div className={classNames('tree-node-row', { 'root-node': isRoot, 'external-node': node.data.isExternal })}>
                {/* Лінії відступу для не-кореневих елементів малюються через CSS/HTML структуру батька */}

                {/* Бейдж типу (C, I, S) */}
                <div className={classNames('node-badge', badge.className)} title={badge.title}>
                    {badge.label}
                </div>

                {/* Ім'я */}
                <span className="node-name" title={node.data.fullName}>
                    {node.data.shortName}
                </span>

                {node.data.isExternal && <span className="external-label">(ext)</span>}
            </div>

            {/* Рендер дітей (які насправді є батьками в ієрархії) */}
            {node.parents.length > 0 && (
                <div className="tree-children">
                    {node.parents.map((parent) => (
                        <div key={parent.data.id} className="child-wrapper">
                            {/* Вертикальна лінія і відступ */}
                            <div className="line-connector">
                                <div className="vertical-line"></div>
                            </div>

                            {/* Рекурсія */}
                            <div className="child-content">
                                <TreeNodeItem node={parent} />
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
};