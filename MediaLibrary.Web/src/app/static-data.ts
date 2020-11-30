import { TagsService, TagSet } from './tags.service';

export let allTags: TagSet;

export function staticDataInit(tagsService: TagsService) {
    return async () => {
        allTags = await tagsService.allTags();
    };
}
