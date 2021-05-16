export interface HashTag {
    hash: string;
    tag: string;
}

export interface SearchResult {
    hash: string;
    fileSize: number;
    fileType: string;
    tags: string[];
    rejectedTags: string[];
    paths: string[];
    people: Person[];
    rejectedPeople: Person[];
}

export interface Person {
    name: string;
    aliases: Alias[];
}

export interface Alias {
    name: string;
    site?: string;
}

export interface Rating {
    category: string;
    count: number;
    hash: string;
    value: number;
}

export interface SavedSearch {
    searchId: number;
    name: string;
    query: string;
}

export interface TagInfo {
    tag: string;
    aliases: string[];
    isAbstract: boolean;
    properties: string[];
    parents: TagInfo[];
    children: TagInfo[];
    ancestors: TagInfo[];
    descendants: TagInfo[];
}

export interface TagInfoTransition {
    tag: string;
    aliases: string[];
    isAbstract: boolean;
    properties: string[];
    parents: (string[] | TagInfo[]);
    children: (string[] | TagInfo[]);
    ancestors: (string[] | TagInfo[]);
    descendants: (string[] | TagInfo[]);
}
