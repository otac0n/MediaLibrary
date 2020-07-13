import { TagInfo } from '../schema';
import { TagsService, TagSet } from './tags.service';

export let alltags: TagSet;

export function staticDataInit(tagsService: TagsService) {
    return async () => {
        const allTagsPromise = tagsService.allTags();

        alltags = await allTagsPromise;
    };
}
