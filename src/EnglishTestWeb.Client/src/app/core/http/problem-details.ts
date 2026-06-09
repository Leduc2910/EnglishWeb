export interface ApiProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  code?: string;
  extensions?: {
    code?: string;
  };
}

export function readProblemCode(problem: ApiProblemDetails): string | undefined {
  return problem.code ?? problem.extensions?.code;
}
