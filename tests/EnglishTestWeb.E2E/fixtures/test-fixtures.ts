import { test as base, APIRequestContext, request } from '@playwright/test';

export type TestFixtures = {
  apiContext: APIRequestContext;
};

export const test = base.extend<TestFixtures>({
  apiContext: async ({}, use) => {
    const ctx = await request.newContext({
      baseURL: 'http://localhost:5124',
      extraHTTPHeaders: {
        'Accept': 'application/json',
        'Content-Type': 'application/json',
      },
    });
    await use(ctx);
    await ctx.dispose();
  },
});

export { expect } from '@playwright/test';
