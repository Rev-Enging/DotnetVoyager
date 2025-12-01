// Enums
export type AssemblyTreeNodeType =
    | 'Assembly' | 'Namespace' | 'Class' | 'Interface'
    | 'Struct' | 'Method' | 'Property' | 'Field';

export type AnalysisOverallStatus = 'Pending' | 'Processing' | 'Completed' | 'Failed';
export type AnalysisStepStatus = 'Pending' | 'InProgress' | 'Completed' | 'Failed';

export type InheritanceGraphNodeType = 'Class' | 'Interface' | 'Struct' | 'Enum';

// DTOs
export interface AssemblyReferenceDto {
    name: string;
    version: string;
    culture: string;
    publicKeyToken: string;
}

export interface AssemblyDependenciesDto {
    assemblyName: string;
    version: string;
    culture: string;
    publicKeyToken: string;
    references: AssemblyReferenceDto[];
}

export interface AssemblyMetadataDto {
    assemblyName: string;
    version: string;
    targetFramework: string;
    architecture: string;
    dependencies: string[];
}

export interface AssemblyStatisticsDto {
    namespaceCount: number;
    classCount: number;
    interfaceCount: number;
    structCount: number;
    methodCount: number;
}

export interface AssemblyTreeNodeDto {
    name: string;
    type: AssemblyTreeNodeType;
    token: number;
    children?: AssemblyTreeNodeDto[];
}

export interface AssemblyTreeDto {
    root: AssemblyTreeNodeDto;
}

export interface DecompiledCodeDto {
    cSharpCode: string;
    ilCode: string;
}

export interface InheritanceGraphNodeDto {
    id: string;
    tokenId: number;
    fullName: string;
    shortName: string;
    type: InheritanceGraphNodeType;
    isExternal: boolean;
}

export interface InheritanceGraphEdgeDto {
    id: string;
    source: string;
    target: string;
}

export interface InheritanceGraphDto {
    nodes: InheritanceGraphNodeDto[];
    edges: InheritanceGraphEdgeDto[];
}

export interface StepStatusDto {
    stepName: string;
    status: AnalysisStepStatus;
    errorMessage?: string;
    startedUtc?: string;
    completedUtc?: string;
}

export interface AnalysisStatusDto {
    analysisId: string;
    overallStatus: AnalysisOverallStatus;
    lastUpdatedUtc: string;
    steps: StepStatusDto[];
}

export interface UploadAssemblyResultDto {
    analysisId: string;
}
