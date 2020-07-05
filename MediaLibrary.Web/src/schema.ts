export interface HashTag {
    hash: string;
    tag: string;
}

export interface SearchResult {
    hash: string;
    fileSize: number;
    fileType: string;
    paths: string[];
    people: Person[];
    tags: string[];
}

export interface Person {
    name: string;
    aliases: Alias[];
}

export interface Alias {
    name: string;
    site?: string;
}
